namespace API_Althea_systems.Services.Email;

/// <summary>
/// In-memory file attached to an outgoing email — the PDF invoice in phase 3
/// is the primary use case.
///
/// We keep the payload as a byte[] rather than a Stream because attachments
/// are typically small (under 1 MB), already buffered, and a record gives us
/// value semantics for unit tests.
/// </summary>
/// <param name="FileName">User-visible filename (must include extension), e.g. "facture-A1B2C3D4.pdf".</param>
/// <param name="Content">Raw bytes of the file.</param>
/// <param name="ContentType">MIME type, e.g. "application/pdf". MUST be a valid Content-Type or MimeKit will reject it.</param>
public record EmailAttachment(string FileName, byte[] Content, string ContentType);
