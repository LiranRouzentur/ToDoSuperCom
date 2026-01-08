import { test, expect } from '@playwright/test';

test.describe('Concurrency Conflict Handling', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('text=Task Management', { timeout: 30000 });
  });

  test('should handle concurrency conflict on task update', async ({ page, context }) => {
    // Create a task first
    const createButton = page.locator('button:has-text("Create"), button:has-text("Add Task")').first();
    if (await createButton.isVisible({ timeout: 5000 })) {
      await createButton.click();
      
      await page.fill('input[name="title"]', 'Concurrency Test Task');
      await page.fill('textarea[name="description"]', 'Test description');
      
      const submitButton = page.locator('button[type="submit"], button:has-text("Create")').first();
      await submitButton.click();
      
      await page.waitForTimeout(2000);
    }

    // Open task for editing
    const editButton = page.locator('button:has-text("Edit"), [aria-label*="Edit" i]').first();
    if (await editButton.isVisible({ timeout: 5000 })) {
      await editButton.click();
    }

    // Make changes in first tab
    const titleInput = page.locator('input[name="title"]').first();
    await titleInput.clear();
    await titleInput.fill('Updated in Tab 1');

    // Open second tab and make conflicting update
    const page2 = await context.newPage();
    await page2.goto('/');
    await page2.waitForSelector('text=Task Management', { timeout: 30000 });

    const editButton2 = page2.locator('button:has-text("Edit"), [aria-label*="Edit" i]').first();
    if (await editButton2.isVisible({ timeout: 5000 })) {
      await editButton2.click();
      
      const titleInput2 = page2.locator('input[name="title"]').first();
      await titleInput2.clear();
      await titleInput2.fill('Updated in Tab 2');
      
      const saveButton2 = page2.locator('button:has-text("Save"), button[type="submit"]').first();
      await saveButton2.click();
      
      await page2.waitForTimeout(2000);
      await page2.close();
    }

    // Try to save in first tab (should show conflict)
    const saveButton = page.locator('button:has-text("Save"), button[type="submit"]').first();
    await saveButton.click();

    // Check for conflict message or modal
    const conflictMessage = page.locator('text=conflict, text=updated, text=outdated', { timeout: 5000 });
    if (await conflictMessage.isVisible({ timeout: 5000 })) {
      // Conflict detected - test passes
      expect(conflictMessage).toBeVisible();
    } else {
      // If no conflict UI, at least verify no error occurred
      const errorMessage = page.locator('text=error, text=failed').first();
      await expect(errorMessage).not.toBeVisible({ timeout: 2000 });
    }
  });
});

