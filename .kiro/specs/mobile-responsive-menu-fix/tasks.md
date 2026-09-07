# Implementation Plan

- [x] 1. Write bug condition exploration test (BEFORE implementing the fix)
  - **Property 1: Bug Condition** - Navegação inferior ausente do MainLayout em mobile
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists (nenhum `nav.bottom-nav` no DOM do `MainLayout`)
  - **Scoped PBT Approach**: The bug is deterministic (component simply never mounted), so scope the property to the concrete failing render: rendering `MainLayout` produces no `nav.bottom-nav`. Use FsCheck over the 5 route destinations (`/`, `/produtos`, `/sobre`, `/fornecedores`, `/contato`) as the "current route" input to show the absence holds for all navigation contexts.
  - Add a new bUnit/xUnit test file in `MaterDomus.Tests/Unit/` (e.g. `MainLayoutBottomNavTests.cs`), namespace `MaterDomus.Tests.Unit`, referencing `MaterDomus.Web.Shared` (matches existing `BottomNavRenderTests.cs`)
  - Render `MainLayout` via bUnit with a test `@Body` (use `ctx.RenderComponent<MainLayout>(...)` with a `Body` render fragment); register any required services (`FakeNavigationManager` is provided by `Bunit.TestContext`)
  - Test case 1 — **BottomNav ausente**: assert `cut.FindAll("nav.bottom-nav")` is empty (from `isBugCondition`: `bottomNavExistsInDom == FALSE`). EXPECTED to FAIL on unfixed code.
  - Test case 2 — **Destinos ausentes**: assert the rendered layout contains no bottom-nav links for `/`, `/produtos`, `/sobre`, `/fornecedores`, `/contato`. EXPECTED to FAIL on unfixed code.
  - Test case 3 — **Controle (BottomNav isolado)**: render `BottomNav` directly and assert it produces `nav.bottom-nav` with 5 links — this SHOULD PASS even on unfixed code, proving the defect is the missing mount in `MainLayout`, not the component itself
  - Run the tests on UNFIXED code: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - **EXPECTED OUTCOME**: Test cases 1 and 2 FAIL (this is correct - it proves the bug exists); test case 3 PASSES
  - Document counterexamples found (e.g., "rendering MainLayout for route '/' yields 0 elements matching nav.bottom-nav; BottomNav rendered in isolation yields 5 links")
  - Mark task complete when the test is written, run, and the failure is documented
  - _Requirements: 1.1, 1.2, 1.3_

- [x] 2. Write preservation property tests (BEFORE implementing the fix)
  - **Property 2: Preservation** - Desktop e restante do layout inalterados
  - **IMPORTANT**: Follow observation-first methodology - observe behavior on UNFIXED code, then encode it
  - Add the preservation tests to the same test file created in task 1 (`MaterDomus.Tests/Unit/MainLayoutBottomNavTests.cs`)
  - Observe on UNFIXED `MainLayout`: header `nav` renders with 5 links (`/`, `/produtos`, `/sobre`, `/fornecedores`, `/contato`); `header.header`, `main` (with `@Body`), `footer.footer`, and `PageViewTracker` are present in that order
  - Observe in `wwwroot/css/site.css`: `.bottom-nav { display: none; }` exists in desktop scope and `.header nav { display: none; }` exists inside `@media (max-width: 768px)`
  - Test case 1 — **Preservação da nav do cabeçalho** (Req 3.1): assert `MainLayout` renders exactly one `header nav` containing 5 links with the 5 expected `href` values. Property-based over current route: assert the header nav and its 5 links are present regardless of active route.
  - Test case 2 — **Preservação do padrão de ocultação** (Req 3.2): read `wwwroot/css/site.css` and assert it still contains `.bottom-nav { display: none; }` (desktop) and `.header nav { display: none; }` within the `@media (max-width: 768px)` block. (Locate the CSS relative to the test assembly / project root.)
  - Test case 3 — **Preservação de cabeçalho/logo, @Body e rodapé** (Req 3.3): assert `header.header .logo img`, a `main` containing the test `@Body` marker, and `footer.footer` are all present and in the same structural order after render
  - Test case 4 — **Preservação do PageViewTracker** (Req 3.4): assert `PageViewTracker` is still rendered as the first component in the layout tree (presence and mount order unchanged)
  - Run the tests on UNFIXED code: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - **EXPECTED OUTCOME**: All preservation tests PASS (this confirms the baseline behavior to preserve)
  - Mark task complete when the tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 3. Fix for navegação inferior ausente em mobile (montar BottomNav no MainLayout)

  - [x] 3.1 Implement the fix
    - Edit `Shared/MainLayout.razor` and add `<BottomNav />` after the closing `</footer>` tag (additive, single line of markup)
    - Do NOT modify `Shared/BottomNav.razor` (already complete: 5 `NavLink`, SVG icons, `aria-label`)
    - Do NOT modify `wwwroot/css/site.css` (responsive rules already correct: `.bottom-nav` hidden on desktop, visible via `@media (max-width: 768px)`, `.header nav` hidden on mobile)
    - No `@using` needed — `BottomNav` shares the `Shared` namespace already covered by `_Imports.razor`; no parameters required
    - _Bug_Condition: isBugCondition(input) = input.viewportWidth <= 768 AND headerNavIsHidden AND NOT bottomNavExistsInDom_
    - _Expected_Behavior: renderMainLayout_fixed produces `nav.bottom-nav` fixed to the bottom with 5 navigable destinations and active-item highlight_
    - _Preservation: header nav visible > 768px, `.bottom-nav` hidden > 768px, header/logo/@Body/footer/PageViewTracker unchanged in any viewport_
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 3.2 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Navegação inferior presente e funcional em mobile
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior; when it passes it confirms the fix
    - Run: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
    - **EXPECTED OUTCOME**: Test cases 1 and 2 from task 1 now PASS — `MainLayout` renders `nav.bottom-nav` with 5 destinations (confirms bug is fixed); control test case 3 still passes
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 3.3 Verify preservation tests still pass
    - **Property 2: Preservation** - Desktop e restante do layout inalterados
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
    - **EXPECTED OUTCOME**: All preservation tests PASS (confirms no regressions) — header nav still present, CSS hide rules intact, header/logo/@Body/footer/PageViewTracker unchanged
    - Confirm all tests still pass after the fix (no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 4. Checkpoint - Ensure all tests pass
  - Run the full suite: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - Confirm the bug condition test (Property 1), preservation tests (Property 2), and the existing `BottomNavRenderTests` all pass
  - Ensure the solution builds: `dotnet build MaterDomus.sln`
  - Ensure all tests pass; ask the user if questions arise
