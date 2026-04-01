#!/bin/sh
# Start ollama server in background
ollama serve > /tmp/ollama-serve.log 2>&1 &
SERVE_PID=$!

echo "Waiting for ollama to start..."
for i in $(seq 1 60); do
  if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "Ollama is ready!"
    break
  fi
  echo "Waiting... attempt $i/60"
  sleep 2
done

echo "Pulling llava model (this may take 10-15 minutes)..."
ollama pull llava >> /tmp/ollama-serve.log 2>&1 || true

echo "Setup complete, keeping container alive..."
wait $SERVE_PID
