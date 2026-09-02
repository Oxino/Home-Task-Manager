import { test, expect } from "@playwright/test";

async function selectProfile(page: import("@playwright/test").Page, displayName: string) {
  await page.goto("/");
  await page.getByRole("button", { name: new RegExp(displayName) }).click();
  await expect(page).toHaveURL(/\/week/);
}

test("un participant peut se connecter et voir sa semaine", async ({ page }) => {
  await selectProfile(page, "user1");

  await expect(page.getByRole("button", { name: "Ma semaine" })).toHaveClass(/active/);
  await expect(page.locator("#today-tasks-heading")).toBeVisible();
});

test("un participant peut marquer une tâche comme terminée", async ({ page }) => {
  // Whichever demo profile got assigned a not-yet-completed task this week (assignment/completion is randomized by seed).
  let hasPendingTask = await tryProfileWithTasks(page, "user1");
  if (!hasPendingTask) hasPendingTask = await tryProfileWithTasks(page, "admin1");
  test.skip(!hasPendingTask, "Ni user1 ni admin1 n'ont de tâche à terminer dans le jeu de données seedé.");

  const pendingTasks = page.locator("article.task:not(.completed)");
  const beforeCount = await pendingTasks.count();

  // The seed cancels exactly one instance without marking it completed, so a "pending-looking"
  // task can legitimately be rejected ("a cancelled task instance cannot be completed"); try the
  // next one in that rare case instead of failing the whole test on seed data variance. The UI
  // notice is set optimistically before the request resolves, so check the real HTTP response.
  let completed = false;
  for (let index = 0; index < beforeCount && !completed; index++) {
    const responsePromise = page.waitForResponse(
      (response) => /\/completion$/.test(response.url()) && response.request().method() === "PUT",
    );
    await pendingTasks.nth(index).locator("button.check-button").click();
    completed = (await responsePromise).ok();
  }
  expect(completed).toBe(true);

  // Re-check from a fresh load: completing a task can reorder the list, so comparing the
  // persisted count (rather than tracking one DOM node through the mutation) is what matters here.
  await page.reload();
  await expect(page.locator("#today-tasks-heading")).toBeVisible();
  await expect(pendingTasks).toHaveCount(beforeCount - 1);
});

async function tryProfileWithTasks(page: import("@playwright/test").Page, displayName: string) {
  await page.context().clearCookies();
  await selectProfile(page, displayName);
  // The instances fetch runs client-side right after landing on /week; give it a moment to
  // resolve before counting rendered tasks (networkidle never fires because of Vite's HMR socket).
  await page.waitForTimeout(1000);
  return (await page.locator("article.task:not(.completed)").count()) > 0;
}


test("un administrateur peut créer un modèle de tâche", async ({ page }) => {
  await selectProfile(page, "admin1");
  await page.goto("/tasks");
  await expect(page.getByRole("heading", { name: "Modèles de tâches" })).toBeVisible();

  const uniqueTitle = `Tâche E2E ${Date.now()}`;
  await page.getByRole("button", { name: "+ Nouvelle tâche" }).click();
  // The Title field is the first textbox in the form and is auto-focused on open;
  // getByLabel is unreliable here because the label also wraps a "?" info button.
  await page.locator("form.task-modal").getByRole("textbox").first().fill(uniqueTitle);
  await page.getByRole("button", { name: "Créer la tâche" }).click();

  await expect(page.getByRole("heading", { name: uniqueTitle })).toBeVisible();
});

test("un administrateur voit le panneau de pilotage des affectations", async ({ page }) => {
  await selectProfile(page, "admin1");
  await page.getByRole("button", { name: "Affectations" }).click();
  await expect(page).toHaveURL(/\/assignments/);

  await expect(page.getByRole("button", { name: /Clôturer/ })).toBeVisible();
  await expect(page.getByRole("button", { name: "Générer les instances" })).toBeVisible();
  await expect(page.getByRole("button", { name: /Lancer l.affectation/ })).toBeVisible();
});
