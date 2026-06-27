namespace LiturgiekStatistiek.Application.Interfaces;

/// <summary>
/// Deterministic, rule-based parser that turns a pasted liturgy (or text extracted
/// from a broadcast page) into structured <see cref="ParsedServiceData"/>. No AI is
/// involved; the result pre-fills the manual add form for review before saving.
/// </summary>
public interface ILiturgyParser
{
    /// <param name="text">The liturgy body (multi-line or "/"-separated).</param>
    /// <param name="title">Optional title line (e.g. "ds. X - Zondag 21 juni 14:00 - Kerk").</param>
    ParsedServiceData Parse(string text, string? title = null);
}
