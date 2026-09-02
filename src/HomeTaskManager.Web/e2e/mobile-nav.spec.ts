import { test, expect } from "@playwright/test";

test("le tiroir de navigation mobile s'ouvre et permet de naviguer", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: /user1/ }).click();
  await expect(page).toHaveURL(/\/week/);

  const toggle = page.getByRole("button", { name: "Ouvrir le menu" });
  await expect(toggle).toBeVisible();
  await expect(page.locator("aside.sidebar")).not.toHaveClass(/mobile-open/);

  await toggle.click();
  await expect(page.locator("aside.sidebar")).toHaveClass(/mobile-open/);
  await expect(page.getByRole("button", { name: "Fermer le menu" })).toBeVisible();

  await page.getByRole("button", { name: "Historique" }).click();

  await expect(page).toHaveURL(/\/history/);
  await expect(page.locator("aside.sidebar")).not.toHaveClass(/mobile-open/);
});
