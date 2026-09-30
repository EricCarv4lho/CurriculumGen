using CurriculumGenerator.Entities;
using CurriculumGenerator.Exceptions;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;

namespace CurriculumGenerator.Services
{
    public class CurriculumService
    {
        private readonly PdfFont helveticaBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        private readonly PdfFont helvetica = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        private readonly PdfFont timesBold = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
        private readonly PdfFont timesRoman = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

        private record TemplateConfig(
            string Name,
            DeviceRgb Primary,
            DeviceRgb Secondary,
            DeviceRgb Accent,
            DeviceRgb Link,
            PdfFont HeadingFont,
            PdfFont BodyFont,
            float HeadingSize,
            float BodySize,
            float SectionSpacing
        );

        private TemplateConfig GetConfig(string template) => template switch
        {
            "modern" => new("modern",
                new DeviceRgb(17, 122, 101), new DeviceRgb(52, 73, 94),
                new DeviceRgb(44, 62, 80), new DeviceRgb(41, 128, 185),
                helveticaBold, helvetica, 18, 11, 12),

            "executive" => new("executive",
                new DeviceRgb(25, 35, 55), new DeviceRgb(60, 70, 90),
                new DeviceRgb(44, 62, 80), new DeviceRgb(41, 128, 185),
                timesBold, timesRoman, 22, 11, 18),

            "creative" => new("creative",
                new DeviceRgb(142, 68, 173), new DeviceRgb(231, 76, 60),
                new DeviceRgb(52, 73, 94), new DeviceRgb(52, 152, 219),
                helveticaBold, helvetica, 20, 10, 14),

            _ => new("classic",
                new DeviceRgb(0, 0, 0), new DeviceRgb(40, 40, 40),
                new DeviceRgb(60, 60, 60), new DeviceRgb(0, 0, 0),
                helveticaBold, helvetica, 20, 12, 15),
        };

        private static readonly Dictionary<string, Dictionary<string, string>> SectionHeaders = new()
        {
            ["pt"] = new()
            {
                ["objective"] = "Objetivo",
                ["experience"] = "Experi\u00eAncia",
                ["education"] = "Educa\u00e7\u00e3o",
                ["languages"] = "Idiomas",
                ["skills"] = "Habilidades",
                ["present"] = "Atualmente"
            },
            ["en"] = new()
            {
                ["objective"] = "Objective",
                ["experience"] = "Experience",
                ["education"] = "Education",
                ["languages"] = "Languages",
                ["skills"] = "Skills",
                ["present"] = "Present"
            },
            ["es"] = new()
            {
                ["objective"] = "Objetivo",
                ["experience"] = "Experiencia",
                ["education"] = "Educaci\u00f3n",
                ["languages"] = "Idiomas",
                ["skills"] = "Habilidades",
                ["present"] = "Actualidad"
            },
            ["fr"] = new()
            {
                ["objective"] = "Objectif",
                ["experience"] = "Exp\u00e9rience",
                ["education"] = "\u00c9ducation",
                ["languages"] = "Langues",
                ["skills"] = "Comp\u00e9tences",
                ["present"] = "Actuel"
            },
            ["de"] = new()
            {
                ["objective"] = "Zielsetzung",
                ["experience"] = "Erfahrung",
                ["education"] = "Bildung",
                ["languages"] = "Sprachen",
                ["skills"] = "F\u00e4higkeiten",
                ["present"] = "Derzeitig"
            },
            ["it"] = new()
            {
                ["objective"] = "Obiettivo",
                ["experience"] = "Esperienza",
                ["education"] = "Istruzione",
                ["languages"] = "Lingue",
                ["skills"] = "Competenze",
                ["present"] = "Attualmente"
            }
        };

        private string H(string key, string lang)
        {
            if (SectionHeaders.TryGetValue(lang, out var headers) && headers.TryGetValue(key, out var val))
                return val;
            return SectionHeaders["pt"].GetValueOrDefault(key, key);
        }

        public byte[] GenerateCurriculum(Curriculum curriculum)
            => GenerateCurriculum(curriculum, "classic", "pt");

        public byte[] GenerateCurriculum(Curriculum curriculum, string template, string language = "pt")
        {
            try
            {
                var cfg = GetConfig(template);
                using var memoryStream = new MemoryStream();
                var pdfWriter = new PdfWriter(memoryStream);
                var pdfDoc = new PdfDocument(pdfWriter);
                Document document = new(pdfDoc, iText.Kernel.Geom.PageSize.A4);

                document.Add(CreateHeaderBlock(curriculum, cfg));

                if (curriculum.Sections is { Count: > 0 })
                {
                    foreach (var section in curriculum.Sections)
                        if (!string.IsNullOrWhiteSpace(section.Content))
                            document.Add(CreateSectionBlock(section.Title, section.Content, cfg));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(curriculum.CareerObjective))
                        document.Add(CreateSectionBlock(H("objective", language), curriculum.CareerObjective, cfg));
                    document.Add(CreateExperienceBlock(curriculum, cfg, language));
                    document.Add(CreateEducationBlock(curriculum, cfg, language));
                    document.Add(CreateLanguagesBlock(curriculum, cfg, language));
                    document.Add(CreateSkillsBlock(curriculum, cfg, language));
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception)
            {
                throw new CurriculumException("Ocorreu um erro ao gerar o currículo.");
            }
        }

        private Cell CreateCell(string text, float size, PdfFont font, DeviceRgb color, int colspan = 12)
        {
            var cell = new Cell(1, colspan);
            cell.Add(new Paragraph(text).SetFontSize(size).SetFont(font).SetFontColor(color).SetBorder(Border.NO_BORDER)).SetBorder(Border.NO_BORDER);
            return cell;
        }

        private Table CreateHeaderBlock(Curriculum curriculum, TemplateConfig cfg)
        {
            var t = new Table(12);
            t.SetBorder(Border.NO_BORDER);
            var name = CreateCell(curriculum.FullName, cfg.HeadingSize + 2, cfg.HeadingFont, cfg.Primary);
            var title = CreateCell(curriculum.ProfessionalTitle, cfg.BodySize, cfg.BodyFont, cfg.Secondary);
            var location = CreateCell($"{curriculum.City}, {curriculum.State}", cfg.BodySize - 2, cfg.BodyFont, cfg.Accent);
            var phone = CreateCell(curriculum.PhoneNumber, cfg.BodySize - 2, cfg.BodyFont, cfg.Accent);
            var email = CreateCell(curriculum.Email, cfg.BodySize - 2, cfg.BodyFont, cfg.Link);
            t.AddCell(name); t.AddCell(title); t.AddCell(location); t.AddCell(phone); t.AddCell(email);

            if (curriculum.LinkedinLink != null)
                t.AddCell(CreateCell(curriculum.LinkedinLink, cfg.BodySize - 2, cfg.BodyFont, cfg.Link));
            if (curriculum.GitHubLink != null)
                t.AddCell(CreateCell(curriculum.GitHubLink, cfg.BodySize - 2, cfg.BodyFont, cfg.Link));

            if (cfg.Name == "executive")
            {
                var line = new SolidLine(1.5f);
                line.SetColor(cfg.Primary);
                var sep = new Cell(1, 12);
                sep.Add(new LineSeparator(line));
                sep.SetBorder(Border.NO_BORDER);
                sep.SetPaddingTop(4);
                t.AddCell(sep);
            }

            return t;
        }

        private Table CreateSectionBlock(string title, string content, TemplateConfig cfg)
        {
            var t = new Table(12);
            t.SetMarginTop(cfg.SectionSpacing);
            t.SetBorder(Border.NO_BORDER);

            if (cfg.Name == "creative")
            {
                var titleCell = new Cell(1, 12);
                var bg = new Paragraph(title)
                    .SetFontSize(cfg.HeadingSize - 4)
                    .SetFont(cfg.HeadingFont)
                    .SetFontColor(new DeviceRgb(255, 255, 255))
                    .SetBackgroundColor(cfg.Primary)
                    .SetPaddingLeft(6).SetPaddingRight(6).SetPaddingTop(3).SetPaddingBottom(3);
                titleCell.Add(bg);
                titleCell.SetBorder(Border.NO_BORDER);
                t.AddCell(titleCell);
            }
            else
            {
                t.AddCell(CreateCell(title, cfg.HeadingSize - 4, cfg.HeadingFont, cfg.Primary));
                if (cfg.Name == "classic" || cfg.Name == "modern")
                {
                    var line = new SolidLine(0.5f);
                    line.SetColor(new DeviceRgb(180, 180, 180));
                    var sep = new Cell(1, 12);
                    sep.Add(new LineSeparator(line));
                    sep.SetBorder(Border.NO_BORDER);
                    t.AddCell(sep);
                }
            }

            t.AddCell(CreateCell(content, cfg.BodySize - 2, cfg.BodyFont, cfg.Accent));
            return t;
        }

        private Table CreateExperienceBlock(Curriculum curriculum, TemplateConfig cfg, string language)
        {
            if (curriculum.Experiences == null || curriculum.Experiences.Count == 0)
                return new Table(12).SetMarginTop(0).SetBorder(Border.NO_BORDER);

            var t = new Table(12);
            t.SetMarginTop(cfg.SectionSpacing);
            t.SetBorder(Border.NO_BORDER);
            t.AddCell(CreateCell(H("experience", language), cfg.HeadingSize - 4, cfg.HeadingFont, cfg.Primary));

            if (cfg.Name == "executive")
            {
                var line = new SolidLine(1.5f);
                line.SetColor(cfg.Primary);
                var sep = new Cell(1, 12);
                sep.Add(new LineSeparator(line));
                sep.SetBorder(Border.NO_BORDER);
                t.AddCell(sep);
            }

            foreach (var exp in curriculum.Experiences)
            {
                var start = exp.StartDate?.ToString("MM/yyyy") ?? "";
                var end = exp.EndDate?.ToString("MM/yyyy") ?? H("present", language);
                var role = $"{exp.JobTitle} - {exp.CompanyName}";
                var dates = $"{start} - {end}";

                if (cfg.Name == "creative")
                {
                    var roleCell = new Cell(1, 8);
                    roleCell.Add(new Paragraph(role).SetFontSize(cfg.BodySize).SetFont(cfg.HeadingFont).SetFontColor(cfg.Primary).SetBorder(Border.NO_BORDER));
                    t.AddCell(roleCell);
                    var dateCell = new Cell(1, 4);
                    dateCell.Add(new Paragraph(dates).SetFontSize(cfg.BodySize - 3).SetFont(cfg.BodyFont).SetFontColor(cfg.Accent).SetBorder(Border.NO_BORDER).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                    t.AddCell(dateCell);
                }
                else
                {
                    t.AddCell(CreateCell(role, cfg.BodySize, cfg.HeadingFont, cfg.Secondary));
                    t.AddCell(CreateCell(dates, cfg.BodySize - 4, cfg.BodyFont, cfg.Accent));
                }
                t.AddCell(CreateCell(exp.Description, cfg.BodySize - 2, cfg.BodyFont, cfg.Accent));
            }
            return t;
        }

        private Table CreateEducationBlock(Curriculum curriculum, TemplateConfig cfg, string language)
        {
            if (curriculum.EducationList == null || curriculum.EducationList.Count == 0)
                return new Table(12).SetMarginTop(0).SetBorder(Border.NO_BORDER);

            var t = new Table(12);
            t.SetMarginTop(cfg.SectionSpacing);
            t.SetBorder(Border.NO_BORDER);
            t.AddCell(CreateCell(H("education", language), cfg.HeadingSize - 4, cfg.HeadingFont, cfg.Primary));

            foreach (var edu in curriculum.EducationList)
            {
                var start = edu.StartDate?.ToString("MM/yyyy") ?? "";
                var end = edu.EndDate?.ToString("MM/yyyy") ?? H("present", language);
                t.AddCell(CreateCell($"{edu.Course} - {edu.InstitutionName}", cfg.BodySize, cfg.BodyFont, cfg.Secondary));
                t.AddCell(CreateCell($"{start} - {end}", cfg.BodySize - 4, cfg.BodyFont, cfg.Accent));
            }
            return t;
        }

        private Table CreateLanguagesBlock(Curriculum curriculum, TemplateConfig cfg, string language)
        {
            if (curriculum.Languages == null || curriculum.Languages.Count == 0)
                return new Table(12).SetMarginTop(0).SetBorder(Border.NO_BORDER);

            var t = new Table(12);
            t.SetMarginTop(cfg.SectionSpacing);
            t.SetBorder(Border.NO_BORDER);
            t.AddCell(CreateCell(H("languages", language), cfg.HeadingSize - 4, cfg.HeadingFont, cfg.Primary));

            foreach (var lang in curriculum.Languages)
                t.AddCell(CreateCell($"{lang.Name} - {lang.Proficiency}", cfg.BodySize - 2, cfg.BodyFont, cfg.Accent));

            return t;
        }

        private Table CreateSkillsBlock(Curriculum curriculum, TemplateConfig cfg, string language)
        {
            var t = new Table(12);
            t.SetMarginTop(cfg.SectionSpacing);
            t.SetBorder(Border.NO_BORDER);
            t.AddCell(CreateCell(H("skills", language), cfg.HeadingSize - 4, cfg.HeadingFont, cfg.Primary));

            var skillList = string.Join(" | ", curriculum.SkillSet);
            t.AddCell(CreateCell(skillList, cfg.BodySize - 2, cfg.BodyFont, cfg.Accent));
            return t;
        }

        public byte[] GenerateCurriculumFromText(string translatedText)
            => GenerateStyledCurriculumFromText(translatedText, new StyleOptions(), "en");

        public byte[] GenerateStyledCurriculumFromText(string translatedText, StyleOptions style, string targetLanguage = "en")
        {
            try
            {
                var (primaryColor, accentColor, headerBgColor, headerTextColor) = GetThemeColors(style.Theme);

                using var memoryStream = new MemoryStream();
                var pdfWriter = new PdfWriter(memoryStream);
                var pdfDoc = new PdfDocument(pdfWriter);
                Document document = new(pdfDoc, iText.Kernel.Geom.PageSize.A4);
                document.SetMargins(25, 25, 25, 25);

                var lines = translatedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var remaining = lines.AsEnumerable();

                var firstLine = remaining.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstLine) && IsPlainName(firstLine.Trim()))
                {
                    var nameTable = new Table(12);
                    nameTable.SetBorder(Border.NO_BORDER);
                    nameTable.AddCell(CreateCell(firstLine.Trim(), 22, 1, 12)
                        .SetBorder(Border.NO_BORDER).SetFont(helveticaBold).SetFontColor(primaryColor));
                    document.Add(nameTable);
                    remaining = remaining.Skip(1);
                }

                foreach (var line in remaining)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (IsSectionHeader(trimmed, targetLanguage))
                    {
                        var headerText = trimmed.TrimEnd(':').Trim();
                        var displayText = style.ShowEmojis ? $"{GetSectionEmoji(headerText)} {headerText}" : headerText;
                        AddStyledSectionHeader(document, displayText, style, primaryColor, accentColor, headerBgColor, headerTextColor);
                    }
                    else
                    {
                        var table = new Table(12);
                        table.SetBorder(Border.NO_BORDER);
                        table.AddCell(CreateCell(trimmed, 10, 1, 12).SetBorder(Border.NO_BORDER).SetFontColor(accentColor));
                        table.SetMarginTop(1);
                        document.Add(table);
                    }
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception)
            {
                throw new CurriculumException("Ocorreu um erro ao gerar o currículo traduzido.");
            }
        }

        private static Cell CreateCell(string text, int fontSize, int rowspan = 1, int colspan = 1)
        {
            Cell cell = new(rowspan, colspan);
            cell.Add(
                new Paragraph(text)
                .SetFontSize(fontSize)
                .SetBorder(Border.NO_BORDER)
            );
            return cell;
        }

        private static readonly Dictionary<string, HashSet<string>> HeaderKeywordsByLang = new()
        {
            ["pt"] = new() { "contato", "objetivo", "educação", "formação", "experiência",
                "habilidades", "idiomas", "resumo", "cursos complementares",
                "formação acadêmica", "experiência profissional", "perfil" },
            ["es"] = new() { "contacto", "objetivo", "formación", "educación", "experiencia",
                "habilidades", "idiomas", "resumen", "cursos complementarios",
                "formación académica", "experiencia profesional", "perfil",
                "logros", "certificaciones" },
            ["fr"] = new() { "contact", "objectif", "formation", "éducation", "expérience",
                "compétences", "langues", "résumé", "cours complémentaires",
                "formation académique", "expérience professionnelle", "profil" },
            ["de"] = new() { "kontakt", "ziel", "bildung", "ausbildung", "erfahrung",
                "fähigkeiten", "sprachen", "zusammenfassung", "kurse",
                "akademische ausbildung", "berufserfahrung", "profil" },
            ["it"] = new() { "contatto", "obiettivo", "formazione", "istruzione", "esperienza",
                "competenze", "lingue", "riepilogo", "corsi complementari",
                "formazione accademica", "esperienza professionale", "profilo" },
            ["en"] = new() { "contact", "objective", "education", "experience",
                "skills", "languages", "summary", "courses",
                "academic education", "work experience", "professional experience",
                "profile", "certifications", "achievements" }
        };

        public static bool IsSectionHeader(string trimmed, string lang)
        {
            var lower = trimmed.ToLower().TrimEnd(':').Trim();

            if (HeaderKeywordsByLang.TryGetValue(lang, out var keywords))
            {
                if (keywords.Contains(lower))
                    return true;

                foreach (var kw in keywords)
                {
                    if (kw.Contains(' ') && lower == kw)
                        return true;
                }
            }

            if (trimmed.All(c => !char.IsLetterOrDigit(c)) && trimmed.Length > 2)
                return true;

            if (trimmed.Length < 50 && trimmed.EndsWith(':') && !trimmed.Contains(' '))
                return true;

            if (trimmed.Length >= 3 && trimmed.Length <= 50
                && trimmed.All(c => !char.IsLower(c) && (char.IsLetter(c) || c == ' ' || c == 'Á' || c == 'É' || c == 'Í' || c == 'Ó' || c == 'Ú' || c == 'Ñ' || c == 'Ä' || c == 'Ö' || c == 'Ü' || c == 'ß' || c == 'Ç' || c == 'Ê' || c == 'Â' || c == 'Î' || c == 'Ô'))
                && trimmed.Count(c => c == ' ') <= 2
                && !trimmed.Contains("  "))
                return true;

            return false;
        }

        private static bool IsPlainName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Length > 50) return false;
            if (text.Contains('@')) return false;
            if (text.Any(char.IsDigit)) return false;
            return true;
        }

        private static string GetSectionEmoji(string header)
        {
            var lower = header.ToLower();
            if (lower.Contains("contact") || lower.Contains("contato") || lower.Contains("contacto")) return "📞";
            if (lower.Contains("objective") || lower.Contains("objetivo") || lower.Contains("objetivo")) return "🎯";
            if (lower.Contains("education") || lower.Contains("educação") || lower.Contains("educación") || lower.Contains("formación") || lower.Contains("formação")) return "🎓";
            if (lower.Contains("experience") || lower.Contains("experiência") || lower.Contains("experiencia") || lower.Contains("work")) return "💼";
            if (lower.Contains("skill") || lower.Contains("habilidade") || lower.Contains("habilidades")) return "🛠️";
            if (lower.Contains("language") || lower.Contains("idioma") || lower.Contains("idiomas")) return "🌍";
            if (lower.Contains("summary") || lower.Contains("resumo") || lower.Contains("resumen")) return "📋";
            if (lower.Contains("course") || lower.Contains("curso") || lower.Contains("cursos")) return "📚";
            if (lower.Contains("certification") || lower.Contains("certificado") || lower.Contains("certificaciones")) return "🏅";
            if (lower.Contains("estado") || lower.Contains("país") || lower.Contains("cidade") || lower.Contains("ciudad")) return "📍";
            if (lower.Contains("perfil")) return "👤";
            if (lower.Contains("logro") || lower.Contains("achievement")) return "🏆";
            return "▸";
        }

        private void AddStyledSectionHeader(Document document, string headerText, StyleOptions style,
            DeviceRgb primaryColor, DeviceRgb accentColor, DeviceRgb headerBg, DeviceRgb headerFg)
        {
            switch (style.SectionStyle)
            {
                case "filled":
                    {
                        var headerParagraph = new Paragraph(headerText)
                            .SetFont(helveticaBold)
                            .SetFontSize(11)
                            .SetFontColor(headerFg)
                            .SetBackgroundColor(primaryColor)
                            .SetPaddingLeft(8)
                            .SetPaddingRight(8)
                            .SetPaddingTop(4)
                            .SetPaddingBottom(4)
                            .SetMarginTop(10)
                            .SetMarginBottom(5);
                        document.Add(headerParagraph);
                        break;
                    }
                case "underline":
                    {
                        var header = new Paragraph(headerText)
                            .SetFontSize(13)
                            .SetFont(helveticaBold)
                            .SetFontColor(primaryColor)
                            .SetMarginTop(8)
                            .SetMarginBottom(2);
                        document.Add(header);
                        var line = new SolidLine(0.3f);
                        line.SetColor(new DeviceRgb(180, 180, 180));
                        document.Add(new LineSeparator(line));
                        break;
                    }
                default:
                    {
                        var table = new Table(12);
                        table.SetBorder(Border.NO_BORDER);
                        table.SetMarginTop(8);
                        table.AddCell(CreateCell(headerText, 13, 1, 12)
                            .SetBorder(Border.NO_BORDER).SetFont(helveticaBold).SetFontColor(primaryColor));
                        document.Add(table);
                        break;
                    }
            }
        }

        private static (DeviceRgb primary, DeviceRgb accent, DeviceRgb headerBg, DeviceRgb headerFg) GetThemeColors(string theme)
        {
            var white = new DeviceRgb(255, 255, 255);
            return theme switch
            {
                "modern" => (new DeviceRgb(17, 122, 101), new DeviceRgb(52, 73, 94),
                    new DeviceRgb(17, 122, 101), white),
                "elegant" => (new DeviceRgb(74, 35, 90), new DeviceRgb(52, 73, 94),
                    new DeviceRgb(74, 35, 90), white),
                "minimal" => (new DeviceRgb(86, 101, 115), new DeviceRgb(52, 73, 94),
                    new DeviceRgb(86, 101, 115), white),
                _ => (new DeviceRgb(26, 82, 118), new DeviceRgb(52, 73, 94),
                    new DeviceRgb(26, 82, 118), white),
            };
        }
    }
}
