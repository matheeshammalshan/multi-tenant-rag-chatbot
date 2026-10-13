using Application.Interfaces.Integrations;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Infrastructure.DocumentReaders
{
    public class PdfDocumentReader:IReaders
    {
        public async Task<string[]> CreateText(DocumentDto doc)
        {
            using var ms = new MemoryStream();
            await doc.File.CopyToAsync(ms);
            ms.Position = 0;
        
            var allSentences=new List<string>();
            
            using (var pdf = PdfDocument.Open(ms))
            {
                foreach (var page in pdf.GetPages())
                {
                
                    var sentences = page.Text?
                        .Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    if (sentences != null)
                        allSentences.AddRange(sentences);
                }
            }
            return allSentences.ToArray();
        }

        public bool CheckType(ReaderType type)
        {
            return type==ReaderType.Pdf;
        }
    }
}