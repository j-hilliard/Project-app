using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Services.Planning;

public static class FcoDocumentService
{
    public static string BuildHtml(FcoDocument fco)
    {
        var total = fco.TotalFcoAmount.ToString("C");
        var updatedContract = fco.UpdatedContractValue.HasValue ? fco.UpdatedContractValue.Value.ToString("C") : "&#8212;";
        var status = fco.Status.ToUpperInvariant();
        var taxDisplay = fco.TaxPct.HasValue ? fco.TaxPct.Value.ToString("P1") : "&#8212;";
        var revCompletion = fco.RevisedCompletionDate?.ToString("MM/dd/yyyy") ?? "&#8212;";
        var estRef = fco.LinkedEstimateId?.ToString() ?? "&#8212;";
        var clientApprovalDate = fco.ClientApprovalDate?.ToString("MM/dd/yyyy") ?? "________________________________";
        var contractorApprovalDate = fco.ContractorApprovalDate?.ToString("MM/dd/yyyy") ?? "________________________________";
        var dateDisplay = fco.Date.ToString("MMMM d, yyyy");
        var generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var revisionSection = string.IsNullOrEmpty(fco.RevisionHistory) ? "" :
            $"<div class=\"section-title\">Revision History</div><table><tr><td>{fco.RevisionHistory}</td></tr></table>";

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8"/>
            <style>
              body { font-family: Arial, sans-serif; font-size: 11pt; margin: 40px; color: #111; }
              h1 { font-size: 18pt; text-align: center; margin: 0 0 4px; }
              .subtitle { text-align: center; color: #555; margin: 0 0 24px; font-size: 10pt; }
              .status-badge { display: inline-block; padding: 3px 12px; border-radius: 4px;
                font-size: 9pt; font-weight: bold; letter-spacing: .05em;
                background: #fef9c3; color: #854d0e; border: 1px solid #fcd34d; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              td, th { border: 1px solid #ccc; padding: 6px 10px; font-size: 10pt; vertical-align: top; }
              th { background: #f3f4f6; font-weight: 600; width: 30%; }
              .section-title { background: #1e3a5f; color: #fff; font-weight: bold;
                font-size: 10pt; padding: 5px 10px; letter-spacing: .06em; text-transform: uppercase; }
              .sig-box { border: 1px solid #ccc; height: 60px; margin-top: 4px; }
              .footer { margin-top: 32px; font-size: 9pt; color: #777; text-align: center; border-top: 1px solid #ccc; padding-top: 8px; }
            </style>
            </head>
            <body>
            <h1>FIELD CHANGE ORDER</h1>
            <div class="subtitle">FCO #{{fco.FcoNumber}} &nbsp;|&nbsp; {{dateDisplay}} &nbsp;|&nbsp; <span class="status-badge">{{status}}</span></div>

            <div class="section-title">Project Information</div>
            <table>
              <tr><th>Project Name</th><td>{{fco.ProjectName ?? "&#8212;"}}</td><th>Project Address</th><td>{{fco.ProjectAddress ?? "&#8212;"}}</td></tr>
              <tr><th>Original Contract/Estimate</th><td>#{{estRef}}</td><th>FCO Number</th><td>{{fco.FcoNumber}}</td></tr>
            </table>

            <div class="section-title">Parties</div>
            <table>
              <tr><th>Client</th><td>{{fco.ClientName ?? "&#8212;"}}</td><th>Client Contact</th><td>{{fco.ClientContact ?? "&#8212;"}}</td></tr>
              <tr><th>Contractor</th><td>{{fco.ContractorName ?? "&#8212;"}}</td><th>Contractor Contact</th><td>{{fco.ContractorContact ?? "&#8212;"}}</td></tr>
              <tr><th>Requested By</th><td>{{fco.RequestedBy ?? "&#8212;"}}</td><th>Prepared By</th><td>{{fco.PreparedBy ?? "&#8212;"}}</td></tr>
            </table>

            <div class="section-title">Scope of Change</div>
            <table>
              <tr><th>Description</th><td colspan="3">{{fco.ScopeDescription ?? "&#8212;"}}</td></tr>
              <tr><th>Reason / Basis</th><td colspan="3">{{fco.Reason ?? "&#8212;"}}</td></tr>
              <tr><th>Schedule Impact</th><td>{{fco.ScheduleImpactDays}} days added</td><th>Revised Completion</th><td>{{revCompletion}}</td></tr>
            </table>

            <div class="section-title">Cost Summary</div>
            <table>
              <tr><th>Labor Breakdown</th><td>{{fco.LaborBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Material Breakdown</th><td>{{fco.MaterialBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Equipment Breakdown</th><td>{{fco.EquipmentBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Markup / Overhead / Profit</th><td>{{fco.MarkupPct:P1}}</td></tr>
              <tr><th>Tax</th><td>{{taxDisplay}}</td></tr>
              <tr><th>Total FCO Amount</th><td><strong>{{total}}</strong></td></tr>
              <tr><th>Updated Contract / Estimate Value</th><td><strong>{{updatedContract}}</strong></td></tr>
            </table>

            <div class="section-title">Approvals</div>
            <table>
              <tr>
                <td style="width:50%">
                  <strong>Client Approval</strong><br/>
                  Name: {{fco.ClientApprovalName ?? "________________________________"}}<br/>
                  Date: {{clientApprovalDate}}<br/>
                  <div class="sig-box"></div>
                </td>
                <td style="width:50%">
                  <strong>Contractor Approval</strong><br/>
                  Name: {{fco.ContractorApprovalName ?? "________________________________"}}<br/>
                  Date: {{contractorApprovalDate}}<br/>
                  <div class="sig-box"></div>
                </td>
              </tr>
            </table>

            {{revisionSection}}

            <div class="footer">
              Generated by Stronghold Platform &mdash; {{generatedAt}} UTC
            </div>
            </body>
            </html>
            """;
    }
}
