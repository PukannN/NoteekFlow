using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace NoteekFlow.Controls;

public partial class MarkdownPreviewControl : UserControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, string?>(nameof(Markdown));
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, double>(nameof(Zoom), 1);
    public static readonly StyledProperty<IBrush> MainTextBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(MainTextBrush), Brush.Parse("#D9DEE7"));
    public static readonly StyledProperty<IBrush> CodeBackgroundBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(CodeBackgroundBrush), Brush.Parse("#22262D"));
    public static readonly StyledProperty<IBrush> CodeForegroundBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(CodeForegroundBrush), Brush.Parse("#D9DEE7"));
    public static readonly StyledProperty<IBrush> LinkTextBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(LinkTextBrush), Brush.Parse("#77AADD"));
    public static readonly StyledProperty<IBrush> QuoteBorderBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(QuoteBorderBrush), Brush.Parse("#527BA5"));
    public static readonly StyledProperty<IBrush> DividerBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(DividerBrush), Brush.Parse("#454B55"));
    public static readonly StyledProperty<IBrush> PanelBorderBrushProperty =
        AvaloniaProperty.Register<MarkdownPreviewControl, IBrush>(nameof(PanelBorderBrush), Brush.Parse("#353B45"));

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public double Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }

    public IBrush MainTextBrush { get => GetValue(MainTextBrushProperty); set => SetValue(MainTextBrushProperty, value); }
    public IBrush CodeBackgroundBrush { get => GetValue(CodeBackgroundBrushProperty); set => SetValue(CodeBackgroundBrushProperty, value); }
    public IBrush CodeForegroundBrush { get => GetValue(CodeForegroundBrushProperty); set => SetValue(CodeForegroundBrushProperty, value); }
    public IBrush LinkTextBrush { get => GetValue(LinkTextBrushProperty); set => SetValue(LinkTextBrushProperty, value); }
    public IBrush QuoteBorderBrush { get => GetValue(QuoteBorderBrushProperty); set => SetValue(QuoteBorderBrushProperty, value); }
    public IBrush DividerBrush { get => GetValue(DividerBrushProperty); set => SetValue(DividerBrushProperty, value); }
    public IBrush PanelBorderBrush { get => GetValue(PanelBorderBrushProperty); set => SetValue(PanelBorderBrushProperty, value); }

    public MarkdownPreviewControl()
    {
        InitializeComponent();
        RenderMarkdown();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownProperty
            || change.Property == ZoomProperty
            || change.Property == MainTextBrushProperty
            || change.Property == CodeBackgroundBrushProperty
            || change.Property == CodeForegroundBrushProperty
            || change.Property == LinkTextBrushProperty
            || change.Property == QuoteBorderBrushProperty
            || change.Property == DividerBrushProperty
            || change.Property == PanelBorderBrushProperty)
        {
            RenderMarkdown();
        }
    }

    private void RenderMarkdown()
    {
        var content = this.FindControl<StackPanel>("PreviewContent");
        if (content is null)
        {
            return;
        }

        content.Children.Clear();
        content.MaxWidth = 900 * Zoom;
        content.Spacing = 12 * Zoom;
        content.Margin = new Thickness(48 * Zoom, 32 * Zoom);
        var document = Markdig.Markdown.Parse(Markdown ?? string.Empty, Pipeline);
        foreach (var block in document)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    content.Children.Add(CreateTextBlock(heading.Inline, (30 - (heading.Level - 1) * 3) * Zoom, FontWeight.SemiBold));
                    break;
                case ParagraphBlock paragraph:
                    content.Children.Add(CreateTextBlock(paragraph.Inline, 16 * Zoom, FontWeight.Normal));
                    break;
                case FencedCodeBlock fencedCode:
                    content.Children.Add(CreateCodeBlock(fencedCode.Lines.ToString()));
                    break;
                case CodeBlock code:
                    content.Children.Add(CreateCodeBlock(code.Lines.ToString()));
                    break;
                case ThematicBreakBlock:
                    content.Children.Add(new Border
                    {
                        Height = 1,
                        Background = DividerBrush,
                        Margin = new Thickness(0, 8)
                    });
                    break;
                case ListBlock list:
                    AddList(content, list);
                    break;
                case QuoteBlock quote:
                    AddQuote(content, quote);
                    break;
                case Table:
                    content.Children.Add(new TextBlock
                    {
                        Text = Markdig.Markdown.ToPlainText(block.ToString() ?? string.Empty, Pipeline),
                        Foreground = MainTextBrush,
                        FontSize = 16 * Zoom
                    });
                    break;
            }
        }
    }

    private TextBlock CreateTextBlock(ContainerInline? inline, double fontSize, FontWeight weight)
    {
        var text = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = MainTextBrush,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        if (inline is not null && text.Inlines is not null)
        {
            AddInlines(text.Inlines, inline);
        }
        return text;
    }

    private Border CreateCodeBlock(string code) => new()
    {
        Background = CodeBackgroundBrush,
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(5),
        Padding = new Thickness(14 * Zoom),
        Child = new TextBlock
        {
            Text = code,
            FontFamily = FontFamily.Default,
            FontSize = 13 * Zoom,
            Foreground = CodeForegroundBrush,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        }
    };

    private void AddList(StackPanel content, ListBlock list)
    {
        var index = 1;
        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            foreach (var child in listItem)
            {
                if (child is ParagraphBlock paragraph)
                {
                    var text = CreateTextBlock(paragraph.Inline, 16 * Zoom, FontWeight.Normal);
                    text.Text = $"{(list.IsOrdered ? $"{index++}." : "•")} {text.Text}";
                    text.Margin = new Thickness(18, 0, 0, 0);
                    content.Children.Add(text);
                }
            }
        }
    }

    private void AddQuote(StackPanel content, QuoteBlock quote)
    {
        var quoteContent = new StackPanel { Spacing = 8 };
        foreach (var child in quote)
        {
            if (child is ParagraphBlock paragraph)
            {
                quoteContent.Children.Add(CreateTextBlock(paragraph.Inline, 16 * Zoom, FontWeight.Normal));
            }
        }

        content.Children.Add(new Border
        {
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(14 * Zoom, 8 * Zoom),
            Child = quoteContent
        });
    }

    private void AddInlines(InlineCollection output, ContainerInline container)
    {
        for (var inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    output.Add(new Run(literal.Content.ToString()));
                    break;
                case CodeInline code:
                    output.Add(new Run(code.Content)
                    {
                        FontFamily = FontFamily.Default,
                        Foreground = CodeForegroundBrush,
                        Background = CodeBackgroundBrush
                    });
                    break;
                case LineBreakInline:
                    output.Add(new LineBreak());
                    break;
                case EmphasisInline emphasis:
                    var span = new Span
                    {
                        FontWeight = emphasis.DelimiterCount > 1 ? FontWeight.Bold : FontWeight.Normal,
                        FontStyle = emphasis.DelimiterCount == 1 ? FontStyle.Italic : FontStyle.Normal
                    };
                    AddInlines(span.Inlines, emphasis);
                    output.Add(span);
                    break;
                case LinkInline link:
                    var linkSpan = new Span { Foreground = LinkTextBrush };
                    AddInlines(linkSpan.Inlines, link);
                    output.Add(linkSpan);
                    break;
                case HtmlInline html:
                    output.Add(new Run(html.Tag));
                    break;
            }
        }
    }
}
