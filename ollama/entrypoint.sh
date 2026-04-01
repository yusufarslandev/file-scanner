#!/bin/sh
ollama serve &
sleep 15
ollama pull llava
wait
