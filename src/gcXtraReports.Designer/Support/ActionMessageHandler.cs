using System;
using Caliburn.Micro;
using DevExpress.XtraReports.UI;
using gcExtensions;
using GeniusCode.XtraReports.Design;
using GeniusCode.XtraReports.Designer.Messaging;
using GeniusCode.XtraReports.Runtime;
using GeniusCode.XtraReports.Runtime.Support;

namespace GeniusCode.XtraReports.Designer.Support
{
    public class ActionMessageHandler : IHandle<DataSourceSelectedForReportMessage>, IHandle<ReportActivatedMessage>, IHandle<ReportActivatedBySubreportMessage>, IHandle<DesignPanelPrintPreviewMessage>
    {
        private readonly IDataSourceSetter _dataSourceSetter;
        private readonly IDesignReportMetadataAssociationRepository _metadataAssociationRepository;
        private readonly IReportControllerFactory _reportControllerFactory;

        public ActionMessageHandler(IDataSourceSetter dataSourceSetter, IEventAggregator aggregator, IDesignReportMetadataAssociationRepository metadataAssociationRepository, IReportControllerFactory reportControllerFactory)
        {
            _dataSourceSetter = dataSourceSetter;
            _metadataAssociationRepository = metadataAssociationRepository;
            _reportControllerFactory = reportControllerFactory;
            aggregator.Subscribe(this);
        }

        public void Handle(ReportActivatedMessage message)
        {
            //TODO: add xml later
        }


        /// <summary>
        /// Concatenates all nested DataMember paths to create the Full DataMember Path
        /// </summary>
        /// <param name="band"></param>
        /// <returns></returns>
        private string GetTraversalPath(Band band)
        {
            var path = String.Empty;

            if (band == null)
                return path;

            band.TryAs<XtraReportBase>(report => path = GetFullDataMemberPathForXtraReportBase(report));


            if (band is XtraReportBase == false)
            {
                var report = band.Report;

                // Recurse into Report, which has the DataMember for the Collection
                // Prevent infinite loop, when a report is it's own parent
                if (band != report)
                    path = GetTraversalPath(report);
            }

            band.TryAs<DetailBand>(detail =>
                                       {
                                           // Detail band is a single item
                                           // By Default, Use first element for design-time
                                           path += "[0]";
                                       });

            return path;
        }

        private string GetFullDataMemberPathForXtraReportBase(XtraReportBase report)
        {
            var context = report.GetReportDataContext();
            var path = context.GetDataMemberDisplayName(report.DataSource, report.DataMember);

            var isRootReport = report == report.Report;

            // Keep calling parent reports, until there is no DataMember
            if (!isRootReport && path != String.Empty)
            {
                // Go deeper
                string parentPath = GetTraversalPath(report.Report);
                path = "{0}.{1}".FormatString(parentPath, path);

                // Prevent a period at the beginning
                if (path.StartsWith("."))
                    path = path.Remove(0, 1);
            }
            else
            {
                // If we are at the top, add starting Relation Path from datasource
                var asMyReportBase = report as gcXtraReport;
                if (asMyReportBase != null)
                {
                    var selectedDatasourceDefinition =
                        _metadataAssociationRepository.GetCurrentAssociationForReport(asMyReportBase);

                    if (selectedDatasourceDefinition != null)
                    {
                        // Append parent report
                        var startingReportPath = _metadataAssociationRepository.GetCurrentAssociationForReport(asMyReportBase);
                        path = startingReportPath.TraversalPath;
                    }
                }

            }

            return path;
        }

        public void Handle(ReportActivatedBySubreportMessage message)
        {
            // go to parent
            var parentReport = message.SelectedSubreport.NavigateToBaseReport();
            // get datasource metadata from parent
            var parentDataSourceDefinition = _metadataAssociationRepository.GetCurrentAssociationForReport(parentReport);

            // if no current datasource, there is nothing to pass
            if (parentDataSourceDefinition == null)
                return;

            // get traversal path
            var relativeTraversalPath = GetTraversalPath(message.SelectedSubreport.Band);
            // combine any previous traversal paths on the datasource with current traversal path inside this report
            var path = CombineTraversalPaths(parentDataSourceDefinition, relativeTraversalPath);
            // set datasource on new report
            _dataSourceSetter.SetReportDatasource(message.NewReport, parentDataSourceDefinition, path);
        }

        private string CombineTraversalPaths(IReportDatasourceMetadataWithTraversal traversal, string deeperPathToAdd)
        {
            if (String.IsNullOrWhiteSpace(traversal.TraversalPath))
                return deeperPathToAdd;

            if (String.IsNullOrWhiteSpace(deeperPathToAdd))
                return traversal.TraversalPath;

            return traversal.TraversalPath + "." + deeperPathToAdd;
        }

        public void Handle(DesignPanelPrintPreviewMessage message)
        {
            _reportControllerFactory.GetController(message.DesignPanel.Report).Print(r => r.ShowPreviewDialog(message.DesignPanel.LookAndFeel));
        }

        public void Handle(DataSourceSelectedForReportMessage message)
        {
            _dataSourceSetter.SetReportDatasource(message.Report,
                message.ReportDatasourceMetadataWithTraversal,
                message.ReportDatasourceMetadataWithTraversal.TraversalPath);
        }

    }
}