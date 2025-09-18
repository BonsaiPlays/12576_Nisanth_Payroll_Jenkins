using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;
using PayrollApi.DTOs;
using PayrollApi.Models;
using PayrollApi.Models.Enums;
using PayrollApi.Services;
using PayrollApi.Utils;

[ApiController]
[Route("api/memos")]
[Authorize(Roles = "Employee,HR,HRManager,Admin")]
public class MemoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    /// <summary>
    /// Gets the current user's ID from the authenticated claims.
    /// </summary>
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public MemoController(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    [HttpGet]
    public async Task<IActionResult> GetMemos()
    {
        var memos = await _db.Memos.Where(m => m.UserId == CurrentUserId).ToListAsync();

        return Ok(memos);
    }

    [HttpPost]
    public async Task<IActionResult> AddMemo([FromBody] MemoDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var memo = new Memo
        {
            UserId = userId,
            Date = dto.Date,
            Content = dto.Content,
        };

        _db.Memos.Add(memo);
        await _db.SaveChangesAsync();

        return Ok(memo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMemo(int id, [FromBody] Memo request)
    {
        var memo = await _db.Memos.FirstOrDefaultAsync(m =>
            m.Id == id && m.UserId == CurrentUserId
        );
        if (memo == null)
            return NotFound();

        memo.Content = request.Content;
        memo.Date = request.Date;
        await _db.SaveChangesAsync();
        return Ok(memo);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMemo(int id)
    {
        var memo = await _db.Memos.FirstOrDefaultAsync(m =>
            m.Id == id && m.UserId == CurrentUserId
        );
        if (memo == null)
            return NotFound();

        _db.Memos.Remove(memo);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
