const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const context = await browser.newContext();
  await context.addInitScript(() => { localStorage.setItem('cvplatform.token', 'test-token'); localStorage.setItem('cvplatform.language', 'en'); });
  let role = 'Admin'; let version = 1; let conflict = false; let networkError = false; let saves = 0; let delay = 0; let lastSave; let uploadsEnabled = false;
  const errors = [];
  const field = { id: 'a1', name: 'English level', description: '', dataType: 'Dropdown', categoryId: 'cat', categoryName: 'Languages', isBuiltIn: false, options: [{ id: 'opt', value: 'Advanced', displayOrder: 0 }], version: 1 };
  const nameField = { ...field, id: 'name', name: 'First Name', dataType: 'String', isBuiltIn: true, systemKey: 'firstName', options: [] };
  const emptyValue = { stringValue: null, numericValue: null, dateValue: null, periodStart: null, periodEnd: null, boolValue: null, selectedOptionId: null, imageUrl: null };
  const profile = { id: 'p1', displayName: 'Test User', version: 1, isOwner: true, builtInDefinitions: [nameField], infoDefinitions: [], values: [{ attributeDefinitionId: 'name', value: { ...emptyValue, stringValue: 'Test' }, display: 'Test' }], projects: [], cvs: [] };
  const position = { id: 'pos1', title: 'Engineer', shortDescription: 'Work', company: 'Acme', level: 'Junior', isPublic: true, maxProjects: 3, projectTags: ['react'], attributes: [{ attributeDefinitionId: 'a1', name: field.name, dataType: field.dataType }], accessRules: [], version: 1, canApply: true, myCvId: null };
  const cv = { id: 'cv1', status: 'Draft', positionId: 'pos1', positionTitle: 'Engineer', profileId: 'p1', candidateName: 'Test User', profileVersion: 1, canEdit: true, canLike: false, likedByMe: false, likeCount: 0, missingCount: 0, header: [{ definition: nameField, value: profile.values[0] }], sections: [], projects: [] };
  const requests = [];
  await context.route('**/api/**', async route => {
    const request = route.request(); const url = new URL(request.url()); const path = url.pathname; requests.push(url.pathname + url.search);
    if (!path.startsWith('/api/')) return route.continue();
    let data = {}; let status = 200;
    if (path === '/api/account/me') { if (role === 'Guest') status = 401; data = { profileId: 'p1', email: 'test@example.com', displayName: 'Test User', roles: [role] }; }
    else if (path === '/api/account/external-providers') data = [];
    else if (path === '/api/attributes/categories') data = [{ id: 'cat', name: 'Languages' }];
    else if (path === '/api/attributes/lookup') data = [field];
    else if (path === '/api/attributes' && request.method() === 'GET') data = [field];
    else if (path === '/api/attributes/a1' && request.method() === 'PUT') { lastSave = request.postDataJSON(); status = conflict ? 409 : 200; data = conflict ? { message: 'Version conflict' } : { ...field, ...lastSave, version: 2 }; }
    else if (path === '/api/tags/suggestions') data = ['react'];
    else if (path === '/api/positions/pos1/cvs') data = [];
    else if (path === '/api/positions/pos1' && request.method() === 'GET') data = position;
    else if (path === '/api/positions/pos1' && request.method() === 'PUT') { lastSave = request.postDataJSON(); data = { id: 'pos1', version: 2 }; }
    else if (path === '/api/positions') data = [position];
    else if (path === '/api/profiles/me' || path === '/api/profiles/p1/edit') data = { ...profile, version };
    else if (path === '/api/profiles/p1') data = { ...profile, publishedCvs: [] };
    else if (path === '/api/profiles/p1/autosave' || path === '/api/cvs/cv1/attributes') {
      saves++; lastSave = request.postDataJSON();
      if (networkError) return route.abort('failed');
      if (delay) await new Promise(resolve => setTimeout(resolve, delay));
      if (conflict || lastSave.version !== version) { status = 409; data = { message: 'Version conflict' }; }
      else { data = { version: ++version }; for (const x of lastSave.changes) if (x.attributeDefinitionId === 'name') { profile.values[0].value = x.value; profile.values[0].display = x.value.stringValue; } }
    }
    else if (path === '/api/cvs/cv1') data = { ...cv, profileVersion: version, canEdit: role !== 'Recruiter' };
    else if (path === '/api/cvs/me') data = [];
    else if (path === '/api/users') data = [{ id: 'u1', profileId: 'p1', email: 'test@example.com', displayName: 'Test', roles: ['Candidate'], registeredAt: new Date().toISOString(), isBlocked: false }];
    else if (path.startsWith('/api/discussions/')) data = [{ id: 'post', authorProfileId: 'p1', authorName: 'Test User', content: 'Hello', createdAt: new Date().toISOString() }];
    else if (path === '/api/search') data = { query: '', positions: [position], cvs: [] };
    else if (path === '/api/uploads/configuration') data = { enabled: uploadsEnabled, cloudName: 'test', uploadPreset: 'test-preset' };
    else if (path === '/api/positions/latest' || path === '/api/positions/popular' || path === '/api/tags') data = [];
    await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(data) });
  });
  const page = await context.newPage(); page.on('pageerror', error => errors.push(error.message));
  const go = path => page.goto('http://127.0.0.1:5173' + path);
  await go('/register'); await page.getByLabel('Are you a recruiter?').check();
  assert.equal(await page.getByText('Continue with Google').count(), 0);
  await page.setViewportSize({ width: 390, height: 844 }); await page.getByRole('searchbox').count();
  assert(await page.locator('.global-search input').isVisible());
  assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await page.screenshot({ path: process.env.TEMP + '/cvplatform-registration-mobile.png', fullPage: true });
  await page.setViewportSize({ width: 1280, height: 900 });
  await go('/positions/pos1/edit'); await page.getByLabel('Title', { exact: true }).fill('Updated engineer');
  await page.getByRole('button', { name: 'Save position' }).click();
  await page.waitForTimeout(300); assert.equal(lastSave.version, 1); assert.equal(lastSave.title, 'Updated engineer');
  await page.getByLabel('Available to all signed-in users').uncheck();
  await page.getByLabel('Select attribute').nth(1).selectOption('a1');
  await page.getByLabel('Value', { exact: true }).selectOption('opt');
  await page.getByRole('button', { name: 'Add rule' }).click();
  await page.getByRole('button', { name: 'Save position' }).click(); await page.waitForTimeout(300);
  assert.equal(lastSave.accessRules[0].comparisonValue, 'opt');
  await page.screenshot({ path: process.env.TEMP + '/cvplatform-position-editor.png', fullPage: true });
  await go('/attributes'); await page.getByRole('checkbox', { name: 'a1', exact: true }).check(); await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await page.getByLabel('Options, one per line').fill('Advanced\nBeginner'); conflict = true;
  await page.getByRole('button', { name: 'Save', exact: true }).click(); await page.getByText('Version conflict', { exact: true }).waitFor();
  assert.equal(await page.getByLabel('Options, one per line').inputValue(), 'Advanced\nBeginner'); conflict = false;
  await go('/users'); await page.getByRole('checkbox', { name: 'u1', exact: true }).check(); assert(await page.getByRole('button', { name: 'Block', exact: true }).isEnabled());
  await go('/positions/pos1'); await page.getByRole('tab', { name: 'Discussion' }).click(); await page.getByRole('link', { name: 'Test User', exact: true }).waitFor();
  await go('/search?tag=react'); await page.getByRole('link', { name: 'Engineer', exact: true }).waitFor(); assert(requests.includes('/api/search?q=&tag=react'));
  await go('/profile'); const input = page.locator('input').filter({ hasNot: page.locator('[type=hidden]') });
  const name = page.locator('.tab-pane.active input').first(); await name.fill('Unsaved');
  let prompted = false; page.once('dialog', dialog => { prompted = true; return dialog.dismiss(); });
  await page.locator('.sidebar-nav').getByRole('link', { name: 'Positions', exact: true }).click();
  assert(prompted); assert(page.url().endsWith('/profile'));
  delay = 1800; const before = saves; await name.fill('First');
  await page.waitForFunction(() => true); while (saves === before) await page.waitForTimeout(100);
  await name.fill('Second'); await page.waitForTimeout(2000); assert.equal(await name.inputValue(), 'Second');
  delay = 0; await page.waitForTimeout(7500); assert.equal(profile.values[0].value.stringValue, 'Second');
  const second = await context.newPage(); await second.goto('http://127.0.0.1:5173/profile'); await second.locator('.tab-pane.active input').first().waitFor();
  await name.fill('Tab one'); await page.waitForTimeout(7500);
  await second.locator('.tab-pane.active input').first().fill('Tab two'); await second.waitForTimeout(7500);
  assert(await second.getByRole('status').filter({ hasText: 'Version conflict' }).isVisible()); assert.equal(await second.locator('.tab-pane.active input').first().inputValue(), 'Tab two');
  await second.close();
  await go('/cvs/cv1'); networkError = true; await page.locator('td input').fill('Network draft'); await page.waitForTimeout(7500);
  assert.equal(await page.locator('td input').inputValue(), 'Network draft'); networkError = false;
  await page.waitForTimeout(7500); assert.equal(profile.values[0].value.stringValue, 'Network draft');
  role = 'Recruiter'; await go('/cvs/cv1'); await page.getByRole('heading', { name: 'Personal details' }).waitFor(); assert.equal(await page.locator('td input').count(), 0);
  role = 'Candidate'; await go('/users'); await page.waitForURL('http://127.0.0.1:5173/');
  await go('/positions/new'); await page.waitForURL('http://127.0.0.1:5173/');
  role = 'Guest'; await go('/cvs'); await page.waitForURL('**/login'); assert(await page.locator('.global-search input').isVisible());
  role = 'Candidate'; uploadsEnabled = true;
  profile.builtInDefinitions.push({ ...nameField, id: 'photo', name: 'Photo', dataType: 'Image', systemKey: 'Photo' });
  let cloudUpload = false;
  await context.route('https://api.cloudinary.com/**', async route => { cloudUpload = true; assert(route.request().postData().includes('test-preset')); await route.fulfill({ contentType: 'application/json', body: JSON.stringify({ secure_url: 'https://res.cloudinary.com/test/image/upload/photo.png' }) }); });
  await go('/profile'); await page.getByText('Drop an image or click to choose').waitFor();
  await page.locator('input[type=file]').setInputFiles({ name: 'photo.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jJb8AAAAASUVORK5CYII=', 'base64') });
  await page.getByLabel('Image URL').inputValue(); await page.waitForTimeout(400); assert(cloudUpload); assert((await page.getByLabel('Image URL').inputValue()).startsWith('https://res.cloudinary.com/'));
  await page.getByLabel('Language', { exact: true }).selectOption('ru'); assert(await page.getByRole('tab', { name: 'Обо мне' }).isVisible());
  await page.getByRole('button', { name: 'Переключить тему' }).click(); assert.equal(await page.locator('html').getAttribute('data-bs-theme'), 'dark');
  assert.deepEqual(errors, []);
  console.log('PASS: mobile search, registration, disabled OAuth, position edit/rules/version, attribute conflict, admin toolbar, discussion links, tag filter, navigation guard, in-flight edits, two-tab conflict, network retry, recruiter read-only CV. API mocked.');
  await browser.close();
})().catch(error => { console.error(error); process.exit(1); });
