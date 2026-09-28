#!/bin/bash
# Quick start script for running Gift Planner in browser

set -e

echo "🎁 Gift Planner - Web Browser"
echo "============================="
echo ""

# Generate JWT signing key if not provided
if [ -z "$JWT_SIGNING_KEY" ]; then
    echo "📝 Generating JWT signing key..."
    export JWT_SIGNING_KEY=$(openssl rand -hex 32)
    echo "✅ Key generated: ${JWT_SIGNING_KEY:0:16}..."
else
    echo "✅ Using provided JWT_SIGNING_KEY"
fi

echo ""
echo "🐳 Starting Docker Compose services..."
echo "   - PostgreSQL (5432)"
echo "   - Redis (6379)"
echo "   - API (5080)"
echo "   - Client (8100)"
echo ""

docker compose up --build

echo ""
echo "✨ Gift Planner is ready!"
echo ""
echo "📍 Open in browser: http://localhost:8100"
echo ""
echo "Press Ctrl+C to stop services"
