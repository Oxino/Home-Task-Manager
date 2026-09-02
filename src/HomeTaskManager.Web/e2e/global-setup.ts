// Resets the dev database to a deterministic demo dataset (admin1/user1 + templates + history)
// before the suite runs, using the API's own seed endpoint (Development-only, see DevelopmentController).
const apiURL = process.env.E2E_API_URL ?? "http://localhost:5251";

async function waitForApi(timeoutMs: number) {
  const deadline = Date.now() + timeoutMs;
  let lastError: unknown;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(`${apiURL}/api/users`);
      if (response.ok) return;
    } catch (error) {
      lastError = error;
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error(`API not reachable at ${apiURL} before timeout: ${String(lastError)}`);
}

export default async function globalSetup() {
  await waitForApi(90_000);

  const response = await fetch(`${apiURL}/api/development/reset-and-seed-full-demo`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ weeks: 6 }),
  });

  if (!response.ok) {
    throw new Error(`Failed to seed demo data: ${response.status} ${await response.text()}`);
  }
}
