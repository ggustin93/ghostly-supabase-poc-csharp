-- This script resets the database to a clean state before applying the new schema.
-- It is designed to be run multiple times without errors.
-- IMPORTANT: This script explicitly AVOIDS touching the 'c3d-files' bucket.


-- Drop existing public tables in reverse order of dependency to avoid foreign key conflicts.
-- Using `CASCADE` handles dependent objects like foreign key constraints automatically.
DROP TABLE IF EXISTS public."GameSession" CASCADE;
DROP TABLE IF EXISTS public."RehabilitationSession" CASCADE;
DROP TABLE IF EXISTS public."Patient" CASCADE;
DROP TABLE IF EXISTS public."Therapist" CASCADE;
DROP TABLE IF EXISTS public."HospitalSite" CASCADE;

-- Drop the new tables as well, in case this script is run after a partial setup.
DROP TABLE IF EXISTS public.emg_sessions CASCADE;
DROP TABLE IF EXISTS public.therapist_patient_assignments CASCADE;
DROP TABLE IF EXISTS public.patients CASCADE;
DROP TABLE IF EXISTS public.therapists CASCADE;

-- Drop the helper function if it exists.
DROP FUNCTION IF EXISTS get_current_therapist_id();

-- Force delete the test users from the auth schema.
-- This is useful for cleaning up "corrupted" users that are difficult to remove from the UI.
DELETE FROM auth.users WHERE email IN ('michael.chen@ghostly.com');


-- Clean up and delete ONLY the 'emg_data' bucket, leaving all others intact.
DELETE FROM storage.objects WHERE bucket_id = 'emg_data';
DELETE FROM storage.buckets WHERE id = 'emg_data';

-- Note: Policies associated with the dropped tables are removed automatically.
-- Storage policies on 'emg_data' are also implicitly removed when the bucket is deleted.
-- The policies on 'c3d-files' remain untouched. 