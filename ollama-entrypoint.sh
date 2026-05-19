#!/bin/bash
set -eu

MODEL_NAME="${OLLAMA_MODEL:-qwen3.5:2b}"

echo "Starting Ollama server..."
/bin/ollama serve &
OLLAMA_PID=$!

cleanup() {
  echo "Stopping Ollama server..."
  kill "$OLLAMA_PID" 2>/dev/null || true
}
trap cleanup INT TERM

echo "Waiting for Ollama server readiness..."
READY=0
for i in $(seq 1 60); do
  if /bin/ollama list >/dev/null 2>&1; then
    READY=1
    break
  fi
  sleep 1
done

if [ "$READY" -eq 0 ]; then
  echo "Ollama server did not become ready in time."
  wait "$OLLAMA_PID"
  exit 1
fi

echo "Pulling model ${MODEL_NAME}..."
PULL_OK=0
for i in 1 2 3; do
  if /bin/ollama pull "$MODEL_NAME"; then
    echo "Model pulled successfully."
    PULL_OK=1
    break
  fi
  if [ "$i" -lt 3 ]; then
    echo "Pull attempt ${i} failed, retrying in 10 seconds..."
    sleep 10
  fi
done

if [ "$PULL_OK" -eq 0 ]; then
  echo "Model pull failed after retries, continuing with server running."
fi

echo "Ollama service is ready."
wait "$OLLAMA_PID"
