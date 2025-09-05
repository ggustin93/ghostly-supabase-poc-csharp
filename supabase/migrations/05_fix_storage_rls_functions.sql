-- Migration 05: Fix Storage RLS Function Context Issues
-- This addresses potential auth context problems in storage policies
-- Run after: 04_rls_database_only_fix.sql
-- Purpose: Improve user_owns_patient function and add debugging capabilities

-- Step 1: Update user_owns_patient function to use consistent context
-- This ensures the same logic is used in both database and storage contexts
CREATE OR REPLACE FUNCTION public.user_owns_patient(p_code TEXT)
RETURNS BOOLEAN AS $$
DECLARE
    current_therapist_id UUID;
BEGIN
    -- Use the same logic as get_current_therapist_id for consistency
    SELECT id INTO current_therapist_id
    FROM public.user_profiles
    WHERE id = auth.uid() 
    AND role = 'therapist'
    AND active = true
    LIMIT 1;
    
    -- If no valid therapist found, return false
    IF current_therapist_id IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- Check if the current therapist owns the patient
    RETURN EXISTS (
        SELECT 1
        FROM public.patients p
        WHERE p.patient_code = p_code
          AND p.therapist_id = current_therapist_id
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Step 2: Create debugging function for storage context analysis
-- This helps troubleshoot RLS policy issues by showing exactly what the storage policies see
CREATE OR REPLACE FUNCTION public.debug_storage_access(file_path TEXT)
RETURNS JSON AS $$
DECLARE
    patient_code TEXT;
    current_user_id UUID;
    therapist_id UUID;
    owns_patient BOOLEAN;
    result JSON;
BEGIN
    -- Extract patient code from file path (same logic as storage policies)
    patient_code := split_part(file_path, '/', 1);
    
    -- Get current auth context
    current_user_id := auth.uid();
    
    -- Get therapist ID using same logic as get_current_therapist_id
    SELECT id INTO therapist_id
    FROM public.user_profiles
    WHERE id = current_user_id 
    AND role = 'therapist'
    AND active = true
    LIMIT 1;
    
    -- Test ownership using the same function as storage policies
    owns_patient := public.user_owns_patient(patient_code);
    
    -- Build comprehensive debug result
    SELECT json_build_object(
        'file_path', file_path,
        'patient_code', patient_code,
        'current_user_id', current_user_id,
        'therapist_id', therapist_id,
        'owns_patient', owns_patient,
        'auth_context_valid', (current_user_id IS NOT NULL),
        'therapist_profile_found', (therapist_id IS NOT NULL)
    ) INTO result;
    
    RETURN result;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Step 3: Test function to validate RLS setup
-- Usage: SELECT public.test_rls_functions();
CREATE OR REPLACE FUNCTION public.test_rls_functions()
RETURNS TABLE(test_name TEXT, result TEXT, success BOOLEAN) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        'Current user auth context'::TEXT,
        COALESCE(auth.uid()::TEXT, 'NULL')::TEXT,
        (auth.uid() IS NOT NULL)::BOOLEAN
    
    UNION ALL
    
    SELECT 
        'Current therapist profile'::TEXT,
        COALESCE(public.get_current_therapist_id()::TEXT, 'NULL')::TEXT,
        (public.get_current_therapist_id() IS NOT NULL)::BOOLEAN
    
    UNION ALL
    
    SELECT 
        'Patient ownership test (P001)'::TEXT,
        public.user_owns_patient('P001')::TEXT,
        TRUE::BOOLEAN
    
    UNION ALL
    
    SELECT 
        'Patient ownership test (P008)'::TEXT,
        public.user_owns_patient('P008')::TEXT,
        TRUE::BOOLEAN;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Comments for troubleshooting:
-- After applying this migration:
-- 1. Test with: SELECT public.debug_storage_access('P008/private_file.c3d');
-- 2. Validate with: SELECT * FROM public.test_rls_functions();
-- 3. Storage policies should use: user_owns_patient(split_part(name, '/', 1))
-- 4. If storage policies still fail, they may need to be recreated via Dashboard UI