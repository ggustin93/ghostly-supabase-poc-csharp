-- Database RLS Security Fix (Storage policies must be applied separately via Dashboard)
-- This migration only fixes database functions and table policies
-- Storage policies MUST be created through the Supabase Dashboard UI

-- Step 1: Drop ALL dependent policies first, then functions
DO $$
BEGIN
    -- Drop core policies that depend on the functions
    DROP POLICY IF EXISTS "Allow therapists to manage their assigned patients" ON public.patients;
    DROP POLICY IF EXISTS "Allow users to manage their own profile" ON public.user_profiles;
    
    -- Drop therapy_sessions policies (check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'therapy_sessions') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage sessions for assigned patients" ON public.therapy_sessions;
    END IF;
    
    -- Drop emg_sessions policies (from old schema, check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'emg_sessions') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage EMG sessions for assigned patients" ON public.emg_sessions;
    END IF;
    
    -- Drop EMG statistics policies (check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'emg_statistics') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage EMG statistics for assigned patients" ON public.emg_statistics;
    END IF;
    
    -- Drop performance scores policies (check existence first)  
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'performance_scores') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage performance scores for assigned patients" ON public.performance_scores;
    END IF;
    
    -- Drop BFR monitoring policies (check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'bfr_monitoring') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage BFR monitoring for assigned patients" ON public.bfr_monitoring;
    END IF;
    
    -- Drop export history policies (check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'export_history') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage their own export history" ON public.export_history;
    END IF;
    
    -- Drop session settings policies (check existence first)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'session_settings') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage session settings for assigned patients" ON public.session_settings;
    END IF;
    
    -- Now drop the functions safely (all dependencies removed)
    DROP FUNCTION IF EXISTS public.get_current_therapist_id();
    DROP FUNCTION IF EXISTS public.user_owns_patient(text);
    DROP FUNCTION IF EXISTS public.get_current_user_id();
    DROP FUNCTION IF EXISTS public.is_assigned_to_patient(text);
END $$;

-- Step 2: Create corrected helper functions for user_profiles table
CREATE OR REPLACE FUNCTION public.get_current_therapist_id()
RETURNS UUID AS $$
DECLARE
    therapist_uuid UUID;
BEGIN
    -- Query the user_profiles table as per the current schema
    SELECT id INTO therapist_uuid
    FROM public.user_profiles
    WHERE id = auth.uid() 
    AND role = 'therapist'
    AND active = true
    LIMIT 1;
    
    RETURN therapist_uuid;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Helper function to check if current user owns a patient by patient_code
CREATE OR REPLACE FUNCTION public.user_owns_patient(p_code TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    -- Check if the current user is the therapist for the given patient
    RETURN EXISTS (
        SELECT 1
        FROM public.patients p
        WHERE p.patient_code = p_code
          AND p.therapist_id = auth.uid()
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Helper function that returns current user ID with proper type handling
CREATE OR REPLACE FUNCTION public.get_current_user_id()
RETURNS UUID AS $$
BEGIN
    RETURN auth.uid();
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Backward compatibility function (some policies might still reference this)
CREATE OR REPLACE FUNCTION public.is_assigned_to_patient(p_code TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN public.user_owns_patient(p_code);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Step 3: Ensure database RLS policies are correct
-- Re-create user profile policy
DROP POLICY IF EXISTS "Allow users to manage their own profile" ON public.user_profiles;
CREATE POLICY "Allow users to manage their own profile"
ON public.user_profiles FOR ALL
USING (id = auth.uid())
WITH CHECK (id = auth.uid());

-- Re-create patient access policy
DROP POLICY IF EXISTS "Allow therapists to manage their assigned patients" ON public.patients;
CREATE POLICY "Allow therapists to manage their assigned patients"
ON public.patients FOR ALL
USING (therapist_id = public.get_current_therapist_id())
WITH CHECK (therapist_id = public.get_current_therapist_id());

-- Re-create therapy sessions policy (if table exists)
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'therapy_sessions') THEN
        CREATE POLICY "Allow therapists to manage sessions for assigned patients"
        ON public.therapy_sessions FOR ALL
        USING (
            patient_id IN (
                SELECT id FROM public.patients
                WHERE therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            patient_id IN (
                SELECT id FROM public.patients
                WHERE therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;
END $$;

-- Re-create EMG sessions policy (if table exists from old schema)
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'emg_sessions') THEN
        CREATE POLICY "Allow therapists to manage EMG sessions for assigned patients"
        ON public.emg_sessions FOR ALL
        USING (
            patient_id IN (
                SELECT id FROM public.patients
                WHERE therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            patient_id IN (
                SELECT id FROM public.patients
                WHERE therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;
END $$;

-- Additional RLS policies for related tables in current schema
DO $$
BEGIN
    -- EMG Statistics policy (linked via therapy_sessions -> patient)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'emg_statistics') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage EMG statistics for assigned patients" ON public.emg_statistics;
        CREATE POLICY "Allow therapists to manage EMG statistics for assigned patients"
        ON public.emg_statistics FOR ALL
        USING (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;

    -- Performance Scores policy (linked via therapy_sessions -> patient)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'performance_scores') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage performance scores for assigned patients" ON public.performance_scores;
        CREATE POLICY "Allow therapists to manage performance scores for assigned patients"
        ON public.performance_scores FOR ALL
        USING (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;

    -- BFR Monitoring policy (linked via therapy_sessions -> patient)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'bfr_monitoring') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage BFR monitoring for assigned patients" ON public.bfr_monitoring;
        CREATE POLICY "Allow therapists to manage BFR monitoring for assigned patients"
        ON public.bfr_monitoring FOR ALL
        USING (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;

    -- Export History policy (therapists can only see their own exports)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'export_history') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage their own export history" ON public.export_history;
        CREATE POLICY "Allow therapists to manage their own export history"
        ON public.export_history FOR ALL
        USING (exported_by = public.get_current_therapist_id())
        WITH CHECK (exported_by = public.get_current_therapist_id());
    END IF;

    -- Session Settings policy (linked via therapy_sessions -> patient)
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'session_settings') THEN
        DROP POLICY IF EXISTS "Allow therapists to manage session settings for assigned patients" ON public.session_settings;
        CREATE POLICY "Allow therapists to manage session settings for assigned patients"
        ON public.session_settings FOR ALL
        USING (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        )
        WITH CHECK (
            session_id IN (
                SELECT ts.id FROM public.therapy_sessions ts
                JOIN public.patients p ON ts.patient_id = p.id
                WHERE p.therapist_id = public.get_current_therapist_id()
            )
        );
    END IF;
END $$;

-- Step 4: Test functions work correctly
-- Run these manually to verify:
-- SELECT get_current_therapist_id(); -- Should return therapist UUID when logged in
-- SELECT user_owns_patient('P001'); -- Should return true/false based on assignment
-- SELECT get_current_user_id(); -- Should return current auth user UUID