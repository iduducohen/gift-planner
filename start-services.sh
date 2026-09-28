#!/bin/bash
# Start Gift Planner services with Docker Compose

set -e

echo "🚀 Starting Gift Planner Services..."
echo ""

# Generate JWT signing key if not set
if [ -z "$JWT_SIGNING_KEY" ]; then
    echo "📝 Generating JWT signing key..."
    export JWT_SIGNING_KEY=$(openssl rand -hex 32)
    echo "✅ JWT_SIGNING_KEY generated"
else
    echo "✅ Using provided JWT_SIGNING_KEY"
fi

echo ""
echo "🐳 Starting Docker Compose..."
docker compose up --build

echo ""
echo "✅ Services started!"
echo ""
echo "📍 Access the application at: http://localhost:8100"
echo "🔌 API endpoint: http://localhost:5080/api/v1"
echo "🗄️  Database: localhost:5432 (user: gift, password: gift)"
echo "💾 Cache: localhost:6379"
echo ""
echo "To run tests in another terminal:"
echo "  cd client && npm test"
echo "  cd server && dotnet test tests/GiftPlanner.Application.Tests/"
echo "  cd e2e && npm test"
