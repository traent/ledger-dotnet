namespace Traent.Ledger.Parser {
    public enum BlockType : ulong {
        Policy = 'G',
        Data = 'D',
        Reference = 'R',
        AddAuthors = 'A',
        Ack = 'K',
        AuthorSignature = 'S',
        PreviousBlock = 'P',
        InContext = 'C',
        UpdateContext = 'U',
    }
}
