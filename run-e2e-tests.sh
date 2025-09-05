#!/bin/bash

# Quick RLS Test Script
# This simulates option 5 from the main menu

echo "🔒 RUNNING RLS SECURITY TEST SUITE"
echo "=================================="
echo ""

# Use expect to simulate user input (option 5 for RLS tests)
expect -c "
set timeout 60
spawn dotnet run
expect \"Enter choice (1-6):\"
send \"5\r\"
expect eof
" 2>/dev/null || {
    echo "❌ expect not available, trying alternative approach..."
    echo "5" | dotnet run 2>/dev/null || {
        echo "❌ Unable to run automated test"
        echo "Please manually run: dotnet run"
        echo "Then select option 5: Multi-Therapist RLS Test Suite 🔒"
        exit 1
    }
}

echo ""
echo "🎉 RLS TEST COMPLETED"