#!/bin/sh
set -e

# Start ollama in background
ollama serve &
SERVE_PID=$!

# Wait for ollama to be ready
sleep 20

# Pull the model
echo "Pulling llava model..."
ollama pull llava || echo "Model pull failed or already cached"

# Wait for serve process to stay alive
wait $SERVE_PID
