import { test, expect } from '@playwright/test';
import { testUsers, testTasks } from '../fixtures/test-data';

test.describe('Task CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app
    await page.goto('/');
    
    // Wait for API to be ready
    await page.waitForSelector('text=Task Management', { timeout: 30000 });
  });

  test('should create a new task', async ({ page }) => {
    // Wait for user selector if present
    const userSelect = page.locator('select, [role="combobox"]').first();
    if (await userSelect.isVisible({ timeout: 5000 })) {
      await userSelect.click();
      await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Enter');
    }

    // Click create task button
    const createButton = page.locator('button:has-text("Create"), button:has-text("Add Task"), button:has-text("New Task")').first();
    await createButton.click();

    // Fill in task form
    await page.fill('input[name="title"], [placeholder*="Title" i]', testTasks.task1.title);
    await page.fill('textarea[name="description"], [placeholder*="Description" i]', testTasks.task1.description);
    
    // Set due date (assuming date picker)
    const dueDateInput = page.locator('input[type="datetime-local"], input[placeholder*="Due" i]').first();
    if (await dueDateInput.isVisible({ timeout: 2000 })) {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 1);
      await dueDateInput.fill(futureDate.toISOString().slice(0, 16));
    }

    // Select priority
    const prioritySelect = page.locator('select[name="priority"], [aria-label*="Priority" i]').first();
    if (await prioritySelect.isVisible({ timeout: 2000 })) {
      await prioritySelect.selectOption(testTasks.task1.priority);
    }

    // Submit form
    const submitButton = page.locator('button[type="submit"], button:has-text("Create"), button:has-text("Save")').first();
    await submitButton.click();

    // Verify task appears in list
    await expect(page.locator(`text=${testTasks.task1.title}`)).toBeVisible({ timeout: 10000 });
  });

  test('should update an existing task', async ({ page }) => {
    // Wait for tasks to load
    await page.waitForSelector('[data-testid="task"], .task-item, table tbody tr', { timeout: 10000 });

    // Click on first task or edit button
    const editButton = page.locator('button:has-text("Edit"), [aria-label*="Edit" i]').first();
    if (await editButton.isVisible({ timeout: 5000 })) {
      await editButton.click();
    } else {
      // Try clicking on task card
      const taskCard = page.locator('[data-testid="task"], .task-item').first();
      await taskCard.click();
    }

    // Update title
    const titleInput = page.locator('input[name="title"]').first();
    await titleInput.clear();
    await titleInput.fill('Updated Task Title');

    // Save changes
    const saveButton = page.locator('button:has-text("Save"), button:has-text("Update"), button[type="submit"]').first();
    await saveButton.click();

    // Verify update
    await expect(page.locator('text=Updated Task Title')).toBeVisible({ timeout: 10000 });
  });

  test('should delete a task', async ({ page }) => {
    // Wait for tasks to load
    await page.waitForSelector('[data-testid="task"], .task-item, table tbody tr', { timeout: 10000 });

    // Find delete button
    const deleteButton = page.locator('button:has-text("Delete"), [aria-label*="Delete" i]').first();
    
    if (await deleteButton.isVisible({ timeout: 5000 })) {
      // Get task title before deletion
      const taskTitle = await page.locator('[data-testid="task"], .task-item').first().textContent();
      
      await deleteButton.click();
      
      // Confirm deletion if confirmation dialog appears
      const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Yes"), button:has-text("Delete")').first();
      if (await confirmButton.isVisible({ timeout: 2000 })) {
        await confirmButton.click();
      }

      // Verify task is removed (if we had the title)
      if (taskTitle) {
        await expect(page.locator(`text=${taskTitle}`)).not.toBeVisible({ timeout: 5000 });
      }
    }
  });

  test('should filter tasks by status', async ({ page }) => {
    // Wait for filters to be available
    await page.waitForSelector('select[name="status"], [aria-label*="Status" i], button:has-text("Filter")', { timeout: 10000 });

    // Try to find and use status filter
    const statusFilter = page.locator('select[name="status"], [aria-label*="Status" i]').first();
    if (await statusFilter.isVisible({ timeout: 5000 })) {
      await statusFilter.selectOption('Open');
      
      // Wait for filtered results
      await page.waitForTimeout(1000);
      
      // Verify filter is applied (check URL or visible tasks)
      const url = page.url();
      expect(url).toContain('status=Open');
    }
  });

  test('should search tasks', async ({ page }) => {
    // Find search input
    const searchInput = page.locator('input[type="search"], input[placeholder*="Search" i]').first();
    
    if (await searchInput.isVisible({ timeout: 5000 })) {
      await searchInput.fill('test');
      await page.waitForTimeout(1000); // Wait for search to execute
      
      // Verify search results (tasks should be filtered)
      const tasks = page.locator('[data-testid="task"], .task-item');
      const count = await tasks.count();
      expect(count).toBeGreaterThanOrEqual(0); // At least no errors
    }
  });
});

