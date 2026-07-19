using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuitionCenter.Models;

public class SubjectsController : Controller
{
    private readonly TuitionCenterDbContext _context;

    public SubjectsController(TuitionCenterDbContext context)
    {
        _context = context;
    }

    // GET: SUBJECTS
    public async Task<IActionResult> Index()
    {
        var subjects = _context.Subjects.Include(s => s.Class);
        return View(await subjects.ToListAsync());
    }

    // GET: SUBJECTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .Include(s => s.Class)
            .FirstOrDefaultAsync(m => m.SubjectId == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    // GET: SUBJECTS/Create
    public IActionResult Create()
    {
        ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SubjectId,ClassId,SubjectName,IsActive")] Subject subject)
    {
        if (ModelState.IsValid)
        {
            _context.Add(subject);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", subject.ClassId);
        return View(subject);
    }

    // GET: SUBJECTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
        {
            return NotFound();
        }
        ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", subject.ClassId);
        return View(subject);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("SubjectId,ClassId,SubjectName,IsActive")] Subject subject)
    {
        if (id != subject.SubjectId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(subject);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(subject.SubjectId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", subject.ClassId);
        return View(subject);
    }

    // GET: SUBJECTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .Include(s => s.Class)
            .FirstOrDefaultAsync(m => m.SubjectId == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject != null)
        {
            _context.Subjects.Remove(subject);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SubjectExists(int id)
    {
        return _context.Subjects.Any(e => e.SubjectId == id);
    }
}