# Codex Review Request: Colab Demo Stability & Logging Issues

## Context
We are preparing a Google Colab demo notebook (`example/pyrope_colab_demo.ipynb`) for the Pyrope project (Garnet + Gemini Sidecar).
We have encountered multiple issues ensuring the Python Sidecar (`src/Pyrope.AISidecar/server.py`) runs correctly in the background and streams logs reliably to the notebook output.

## Current State
- **Notebook**: `example/pyrope_colab_demo.ipynb`
- **Sidecar Entry Point**: `src/Pyrope.AISidecar/server.py`
- **Environment**: Google Colab (Linux, Python 3.10+, .NET 8.0 installed via script)

## Problems Encountered & Attempted Fixes

1.  **`ModuleNotFoundError: 'policy_service_pb2'`**
    -   *Cause*: gRPC stubs are not pre-generated in the repo.
    -   *Fix*: Added a `protoc` generation step in the notebook.
    -   *Issue*: Initial path was wrong (`src/Pyrope.GarnetServer/Protos` vs `src/Protos`). Fixed, but worth checking if this is robust.

2.  **Empty Logs / Silent Failures**
    -   *Symptoms*: The Sidecar process starts (presumably), but `process.stdout.read()` returns empty strings or hangs.
    -   *Attempts*:
        -   Added `logging.basicConfig(level=logging.INFO, force=True)`.
        -   Start process with `python -u` (unbuffered).
        -   Redirect `stdout` to a file (`sidecar.log`) instead of `subprocess.PIPE` to avoid buffer deadlocks.
        -   Use `!cat sidecar.log` to inspect output.
        -   Added explicit `print("DEBUG: ...", flush=True)` at the start of `server.py`.

3.  **`ValueError: I/O operation on closed file`**
    -   *Cause*: Reading from a closed handle/pipe in a finalized cell.
    -   *Fix*: Switched to file-based logging (`open("sidecar.log", "w")`).

4.  **`FutureWarning: google.generativeai`**
    -   *Fix*: Added `warnings.filterwarnings` to `server.py`.

## Review Instructions

Please review the following files:
1.  `example/pyrope_colab_demo.ipynb` (Focus on "Launch Services" and "Check Logs" cells)
2.  `src/Pyrope.AISidecar/server.py` (Focus on logging config and startup logic)

## Questions

1.  **Why might the Sidecar still fail silently?**
    Even with `python -u` and file redirection, we are seeing cases where logs are empty. Are there exceptions (e.g., during import) that wouldn't be caught by our current setup? `stderr=subprocess.STDOUT` is set, so we expect errors in the log file.

2.  **Is file-based logging in Colab the best local approach?**
    Is there a more "Colab-native" way to run a background service and stream its output to a cell (like `%%bash --bg` magic or similar)? The current `subprocess.Popen` + `time.sleep` feels brittle.

3.  **gRPC Generation Robustness**
    Is the explicit `python -m grpc_tools.protoc ...` step in the notebook the best way to handle this dependency in a ephemeral environment like Colab?

4.  **General Stability**
    Are there other potential pitfalls in running a multi-process system (Dotnet Server + Python Sidecar + Redis Client) in a single Colab runtime that we should mitigate (e.g., OOM kills, port conflicts)?

## Files to Review
- `example/pyrope_colab_demo.ipynb`
- `src/Pyrope.AISidecar/server.py`
