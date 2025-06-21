﻿using System.Text;

namespace Blastia.Main.Commands;

/// <summary>
/// Filter out unnecessary text from console logs
/// </summary>
public class FilteredTextWriter : TextWriter
{
    private readonly TextWriter _originalWriter;
    private readonly Func<string, bool> _filter;
    private readonly StringBuilder _lineBuffer = new();
    
    public override Encoding Encoding => _originalWriter.Encoding;
    
    public FilteredTextWriter(TextWriter originalWriter, Func<string, bool> filter)
    {
        _originalWriter = originalWriter;
        _filter = filter;
    }

    public override void Write(char value)
    {
        if (value == '\n')
        {
            var line = _lineBuffer.ToString();
            if (_filter(line))
                _originalWriter.WriteLine(line);
            _lineBuffer.Clear();
        }
        else if (value != '\r')
        {
            _lineBuffer.Append(value);
        }
    }
    
    public override void WriteLine(string? value)
    {
        if (value != null && _filter(value))
            _originalWriter.WriteLine(value);
    }
}