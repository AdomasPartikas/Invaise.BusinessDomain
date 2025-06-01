using Invaise.BusinessDomain.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// QuestPDF document implementation for portfolio performance reports
/// </summary>
public class PortfolioPerformanceDocumentationService(PortfolioPerformanceReportModel model) : IDocument
{
    
    /// <summary>
    /// Composes the PDF document
    /// </summary>
    /// <param name="container">The document container</param>
    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }
    
    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text("INVAISE")
                    .FontSize(24)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);
                    
                column.Item().AlignCenter().Text("Portfolio Performance Report")
                    .FontSize(18)
                    .FontColor(Colors.Grey.Darken2);
            });
        });
    }
    
    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // User information section
            column.Item().PaddingVertical(10).Element(ComposeUserInformation);
            
            // Portfolio summary section
            column.Item().PaddingVertical(10).Element(ComposePortfolioSummary);
            
            // Current portfolio section
            column.Item().PaddingVertical(10).Element(ComposeCurrentPortfolio);
            
            // Performance history section
            column.Item().PaddingVertical(10).Element(ComposePerformanceHistory);
        });
    }
    
    private void ComposeUserInformation(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("User Information").SemiBold().FontSize(14);
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
            
                table.Cell().Text("Name:");
                table.Cell().Text(model.User.DisplayName);
            
                if (model.User.PersonalInfo != null)
                {
                    table.Cell().Text("Email:");
                    table.Cell().Text(model.User.Email);
            
                    // Only show personal info if available
                    table.Cell().Text("Phone:");
                    table.Cell().Text(model.User.PersonalInfo.PhoneNumber);
            
                    table.Cell().Text("Address:");
                    table.Cell().Text(model.User.PersonalInfo.Address);
            
                    table.Cell().Text("City:");
                    table.Cell().Text(model.User.PersonalInfo.City);
            
                    table.Cell().Text("Country:");
                    table.Cell().Text(model.User.PersonalInfo.Country);
                }
            });
        });
    }
    
    private void ComposePortfolioSummary(IContainer container)
    {
        var latestPerformance = model.PerformanceData.OrderByDescending(p => p.Date).FirstOrDefault();
        
        container.Column(column =>
        {
            column.Item().Text("Portfolio Summary").SemiBold().FontSize(14);
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
            
                table.Cell().Text("Portfolio Name:");
                table.Cell().Text(model.Portfolio.Name);
                
                table.Cell().Text("Strategy:");
                table.Cell().Text(model.Portfolio.StrategyDescription.ToString());
                
                table.Cell().Text("Created Date:");
                table.Cell().Text(model.Portfolio.CreatedAt.ToString("dd/MM/yyyy"));
                
                table.Cell().Text("Last Updated:");
                table.Cell().Text(model.Portfolio.LastUpdated?.ToString("dd/MM/yyyy HH:mm") ?? "N/A");
                
                table.Cell().Text("Report Period:");
                table.Cell().Text($"{model.StartDate:dd/MM/yyyy} - {model.EndDate:dd/MM/yyyy}");
                
                if (latestPerformance != null)
                {
                    table.Cell().Text("Current Total Value:");
                    table.Cell().Text($"${latestPerformance.TotalValue:N2}");
                    
                    table.Cell().Text("Number of Stocks:");
                    table.Cell().Text(latestPerformance.TotalStocks.ToString());
                }
            });
        });
    }
    
    private void ComposeCurrentPortfolio(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Current Portfolio Composition").SemiBold().FontSize(14);
            
            if (model.PortfolioStocks.Any())
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Symbol").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Quantity").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Base Value").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Current Value").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Change %").SemiBold();
                    });
                    
                    foreach (var stock in model.PortfolioStocks)
                    {
                        table.Cell().Text(stock.Symbol);
                        table.Cell().Text(stock.Quantity.ToString("N2"));
                        table.Cell().Text($"${stock.TotalBaseValue:N2}");
                        table.Cell().Text($"${stock.CurrentTotalValue:N2}");
                        
                        Color percentageColor = stock.PercentageChange >= 0 ? Colors.Green.Medium : Colors.Red.Medium;
                        table.Cell().Text(text =>
                        {
                            text.Span($"{stock.PercentageChange:N2}%").FontColor(percentageColor);
                        });
                    }
                });
            }
            else
            {
                column.Item().Text("No stocks in portfolio").Italic();
            }
        });
    }
    
    private void ComposePerformanceHistory(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Performance History").SemiBold().FontSize(14);
            
            if (model.PerformanceData.Any())
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Date").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Total Value").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Daily Change %").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Monthly Change %").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Text("Yearly Change %").SemiBold();
                    });
                    
                    foreach (var performance in model.PerformanceData.OrderByDescending(p => p.Date))
                    {
                        table.Cell().Text(performance.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Text($"${performance.TotalValue:N2}");
                        
                        Color dailyColor = performance.DailyChangePercent >= 0 ? Colors.Green.Medium : Colors.Red.Medium;
                        table.Cell().Text(text =>
                        {
                            text.Span($"{performance.DailyChangePercent:N2}%").FontColor(dailyColor);
                        });
                        
                        if (performance.MonthlyChangePercent.HasValue)
                        {
                            Color monthlyColor = performance.MonthlyChangePercent.Value >= 0 ? Colors.Green.Medium : Colors.Red.Medium;
                            table.Cell().Text(text =>
                            {
                                text.Span($"{performance.MonthlyChangePercent:N2}%").FontColor(monthlyColor);
                            });
                        }
                        else
                        {
                            table.Cell().Text("N/A");
                        }
                        
                        if (performance.YearlyChangePercent.HasValue)
                        {
                            Color yearlyColor = performance.YearlyChangePercent.Value >= 0 ? Colors.Green.Medium : Colors.Red.Medium;
                            table.Cell().Text(text =>
                            {
                                text.Span($"{performance.YearlyChangePercent:N2}%").FontColor(yearlyColor);
                            });
                        }
                        else
                        {
                            table.Cell().Text("N/A");
                        }
                    }
                });
            }
            else
            {
                column.Item().Text("No performance data available for the selected period").Italic();
            }
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("Report generated on ");
                    text.Span($"{model.GenerationDate:dd/MM/yyyy HH:mm}").SemiBold();
                });
                
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
                
                column.Item().AlignCenter().Text("© Invaise - All rights reserved").FontSize(8);
            });
        });
    }
} 