-- This script seeds the database with therapist and patient data for RLS testing.
-- It now assumes you have MANUALLY created the auth users in the Supabase dashboard.

-- Enable UUID generation if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

DO $$
DECLARE
    therapist1_user_id UUID;
    therapist2_user_id UUID;
    therapist1_profile_id UUID;
    therapist2_profile_id UUID;
BEGIN
    -- Get user IDs from auth.users. Fail if they don't exist.
    SELECT id INTO therapist1_user_id FROM auth.users WHERE email = 'therapist1@example.com';
    IF therapist1_user_id IS NULL THEN RAISE EXCEPTION 'Manual User Creation Required: therapist1@example.com not found.'; END IF;

    SELECT id INTO therapist2_user_id FROM auth.users WHERE email = 'therapist2@example.com';
    IF therapist2_user_id IS NULL THEN RAISE EXCEPTION 'Manual User Creation Required: therapist2@example.com not found.'; END IF;

    -- Upsert Therapist 1 Profile
    SELECT id INTO therapist1_profile_id FROM public.therapists WHERE user_id = therapist1_user_id;
    IF therapist1_profile_id IS NULL THEN
        INSERT INTO public.therapists (user_id, first_name, last_name)
        VALUES (therapist1_user_id, 'Therapist', 'One')
        RETURNING id INTO therapist1_profile_id;
    END IF;

    -- Upsert Therapist 2 Profile
    SELECT id INTO therapist2_profile_id FROM public.therapists WHERE user_id = therapist2_user_id;
    IF therapist2_profile_id IS NULL THEN
        INSERT INTO public.therapists (user_id, first_name, last_name)
        VALUES (therapist2_user_id, 'Therapist', 'Two')
        RETURNING id INTO therapist2_profile_id;
    END IF;

    -- Upsert Patient 1 assigned to Therapist 1
    IF NOT EXISTS (SELECT 1 FROM public.patients WHERE therapist_id = therapist1_profile_id AND last_name = 'Alpha') THEN
        INSERT INTO public.patients (therapist_id, first_name, last_name, date_of_birth)
        VALUES (therapist1_profile_id, 'Patient', 'Alpha', '1990-01-15');
    END IF;

    -- Upsert Patient 2 assigned to Therapist 2
    IF NOT EXISTS (SELECT 1 FROM public.patients WHERE therapist_id = therapist2_profile_id AND last_name = 'Beta') THEN
        INSERT INTO public.patients (therapist_id, first_name, last_name, date_of_birth)
        VALUES (therapist2_profile_id, 'Patient', 'Beta', '1992-06-20');
    END IF;

    -- Note: Seeding of `emg_sessions` and file uploads will be handled by the C# test setup
    -- to more accurately simulate the application's behavior.

    RAISE NOTICE 'Seeding script for therapists and patients completed successfully.';
END $$;