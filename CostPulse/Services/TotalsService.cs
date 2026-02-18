using System;
using System.Collections.Generic;
using System.Linq;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class TotalsService
    {
        private DateTime _sessionStartTime;

        public TotalsService()
        {
            _sessionStartTime = DateTime.Now;
        }

        public decimal GetSessionTotal(IEnumerable<UsageEntry> entries)
        {
            return entries.Where(e => e.Timestamp >= _sessionStartTime).Sum(e => e.Cost);
        }

        public decimal GetTodayTotal(IEnumerable<UsageEntry> entries)
        {
            var today = DateTime.Today;
            return entries.Where(e => e.Timestamp.Date == today).Sum(e => e.Cost);
        }

        public decimal GetMonthTotal(IEnumerable<UsageEntry> entries)
        {
            var today = DateTime.Today;
            return entries.Where(e => e.Timestamp.Year == today.Year && e.Timestamp.Month == today.Month).Sum(e => e.Cost);
        }

        public int GetTokensToday(IEnumerable<UsageEntry> entries)
        {
            var today = DateTime.Today;
            return entries.Where(e => e.Timestamp.Date == today).Sum(e => e.InputTokens + e.OutputTokens);
        }

        public int GetTokensMonth(IEnumerable<UsageEntry> entries)
        {
            var today = DateTime.Today;
            return entries.Where(e => e.Timestamp.Year == today.Year && e.Timestamp.Month == today.Month).Sum(e => e.InputTokens + e.OutputTokens);
        }

        public List<decimal> GetDailyTotals(IEnumerable<UsageEntry> entries, int days)
        {
            var result = new List<decimal>();
            var today = DateTime.Today;
            for (int i = days - 1; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var cost = entries.Where(e => e.Timestamp.Date == date).Sum(e => e.Cost);
                result.Add(cost);
            }
            return result;
        }
    }
}
