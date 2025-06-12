-- This script initializes the database schema, storage, and security policies for the multi-therapist RLS MVP.
-- This version uses a simplified one-to-many relationship (1 Therapist -> N Patients) and is fully idempotent.

--------------------------------------------------------------------------------
-- 1. STORAGE SETUP
--------------------------------------------------------------------------------

-- Create a new, secure storage bucket for EMG data.
-- This bucket is configured for private access; files can only be accessed through
-- signed URLs or via the storage policies defined below.
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('emg_data', 'emg_data', FALSE, 5242880, ARRAY['application/octet-stream', 'text/plain'])
ON CONFLICT (id) DO NOTHING;


--------------------------------------------------------------------------------
-- 2. DATABASE SCHEMA
--------------------------------------------------------------------------------

-- Table to store therapist profiles.
-- Links to `auth.users` to associate each therapist with an authentication identity.
CREATE TABLE IF NOT EXISTS public.therapists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID UNIQUE NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- The `patients` table now includes a direct foreign key to `therapists`.
CREATE TABLE IF NOT EXISTS public.patients (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    therapist_id UUID NOT NULL REFERENCES public.therapists(id),
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    date_of_birth DATE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- The `therapist_patient_assignments` table is no longer needed.

-- Table to store metadata for EMG session files.
-- Links a patient to a specific file in the `emg_data` storage bucket.
CREATE TABLE IF NOT EXISTS public.emg_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    patient_id UUID NOT NULL REFERENCES public.patients(id) ON DELETE CASCADE,
    file_path TEXT NOT NULL UNIQUE, -- e.g., "patient_uuid/session_uuid.bin"
    recorded_at TIMESTAMPTZ NOT NULL,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);


--------------------------------------------------------------------------------
-- 3. HELPER FUNCTIONS
--------------------------------------------------------------------------------

-- Helper function to get the therapist_id for the currently authenticated user.
-- Reduces code duplication in RLS policies.
CREATE OR REPLACE FUNCTION public.get_current_therapist_id()
RETURNS UUID AS $$
DECLARE
    therapist_uuid UUID;
BEGIN
    SELECT id INTO therapist_uuid
    FROM public.therapists
    WHERE user_id = auth.uid()
    LIMIT 1;
    RETURN therapist_uuid;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;


--------------------------------------------------------------------------------
-- 4. RLS (ROW-LEVEL SECURITY) POLICIES
--------------------------------------------------------------------------------

ALTER TABLE public.therapists ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.patients ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.emg_sessions ENABLE ROW LEVEL SECURITY;

-- Drop existing policies before creating new ones to ensure idempotency.
DROP POLICY IF EXISTS "Allow therapists to manage their own profile" ON public.therapists;
CREATE POLICY "Allow therapists to manage their own profile"
ON public.therapists FOR ALL
USING (user_id = auth.uid())
WITH CHECK (user_id = auth.uid());

DROP POLICY IF EXISTS "Allow therapists to manage their assigned patients" ON public.patients;
CREATE POLICY "Allow therapists to manage their assigned patients"
ON public.patients FOR ALL
USING (therapist_id = public.get_current_therapist_id())
WITH CHECK (therapist_id = public.get_current_therapist_id());

DROP POLICY IF EXISTS "Allow therapists to manage EMG sessions for assigned patients" ON public.emg_sessions;
CREATE POLICY "Allow therapists to manage EMG sessions for assigned patients"
ON public.emg_sessions FOR ALL
USING (
    patient_id IN (
        SELECT id FROM public.patients
        WHERE therapist_id = public.get_current_therapist_id()
    )
);


--------------------------------------------------------------------------------
-- 5. STORAGE POLICIES
--------------------------------------------------------------------------------

-- Helper function to check if a therapist is assigned to a patient.
-- This simplifies the storage policies.
CREATE OR REPLACE FUNCTION public.is_assigned_to_patient(patient_uuid UUID)
RETURNS BOOLEAN AS $$
DECLARE
    is_assigned BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM public.patients
        WHERE id = patient_uuid AND therapist_id = public.get_current_therapist_id()
    ) INTO is_assigned;
    RETURN is_assigned;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- The policies are now simpler and rely on the `is_assigned_to_patient` helper function.
-- The file's path is expected to be `patient-uuid/filename`.

DROP POLICY IF EXISTS "Allow therapists to list the emg_data bucket" ON storage.objects;
CREATE POLICY "Allow therapists to list the emg_data bucket"
ON storage.objects FOR SELECT
USING (bucket_id = 'emg_data');

DROP POLICY IF EXISTS "Allow therapists to access files for assigned patients" ON storage.objects;
CREATE POLICY "Allow therapists to access files for assigned patients"
ON storage.objects FOR SELECT
USING (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1]::UUID)
);

DROP POLICY IF EXISTS "Allow therapists to upload files for assigned patients" ON storage.objects;
CREATE POLICY "Allow therapists to upload files for assigned patients"
ON storage.objects FOR INSERT
WITH CHECK (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1]::UUID)
);

DROP POLICY IF EXISTS "Allow therapists to update files for assigned patients" ON storage.objects;
CREATE POLICY "Allow therapists to update files for assigned patients"
ON storage.objects FOR UPDATE
USING (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1]::UUID)
);

DROP POLICY IF EXISTS "Allow therapists to delete files for assigned patients" ON storage.objects;
CREATE POLICY "Allow therapists to delete files for assigned patients"
ON storage.objects FOR DELETE
USING (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1]::UUID)
);