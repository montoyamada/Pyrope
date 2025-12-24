FROM python:3.10-slim

WORKDIR /app

COPY src/Pyrope.AISidecar/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/Pyrope.AISidecar/ .

# Expose GRPC port
EXPOSE 50051

CMD ["python", "server.py"]
