
using Microsoft.AspNetCore.Mvc;
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
        return View(await _context.Subjects.ToListAsync());
    }

    // GET: SUBJECTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    // GET: SUBJECTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SUBJECTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Description,IsActive,CreatedDate,ClassSchedules,Courses,TeacherSubjects")] Subject subject)
    {
        if (ModelState.IsValid)
        {
            _context.Add(subject);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
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
        return View(subject);
    }

    // POST: SUBJECTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Description,IsActive,CreatedDate,ClassSchedules,Courses,TeacherSubjects")] Subject subject)
    {
        if (id != subject.Id)
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
                if (!SubjectExists(subject.Id))
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
            .FirstOrDefaultAsync(m => m.Id == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    // POST: SUBJECTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject != null)
        {
            _context.Subjects.Remove(subject);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SubjectExists(int? id)
    {
        return _context.Subjects.Any(e => e.Id == id);
    }
}
