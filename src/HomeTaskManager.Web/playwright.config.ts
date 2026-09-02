import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:5173";
const apiURL = process.env.E2E_API_URL ?? "http://localhost:5251";

export default defineConfig({
  testDir: "./e2e",
  globalSetup: "./e2e/global-setup.ts",
  // Real DB mutations (profile/task creation) run against a shared seeded dataset: keep it deterministic.
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: "list",
  use: {
    baseURL,
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "Desktop Chrome",
      testMatch: /critical-paths\.spec\.ts/,
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "Mobile Chrome",
      testMatch: /mobile-nav\.spec\.ts/,
      use: { ...devices["Pixel 5"] },
    },
  ],
  webServer: [
    {
      command: `dotnet run --project ../HomeTaskManager.Api/HomeTaskManager.Api.csproj --urls ${apiURL}`,
      url: `${apiURL}/api/users`,
      reuseExistingServer: !process.env.CI,
      env: { ASPNETCORE_ENVIRONMENT: "Development" },
      timeout: 120_000,
    },
    {
      command: "npm run dev",
      url: baseURL,
      reuseExistingServer: !process.env.CI,
      timeout: 60_000,
    },
  ],
});
