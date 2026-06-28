using Microsoft.Web.WebView2.Core;
using System.Windows.Controls;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class PortfolioView : UserControl
    {
        public PortfolioView()
        {
            InitializeComponent();
            InitializeWebView();
        }

        // ───────────────────────────────────────
        // WebView2 초기화
        // PDF 경로가 바뀔 때마다 자동으로 미리보기 갱신
        // ───────────────────────────────────────
        private async void InitializeWebView()
        {
            await PdfPreview.EnsureCoreWebView2Async();

            if (DataContext is PortfolioViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PortfolioViewModel.GeneratedPdfPath)
                        && vm.GeneratedPdfPath != null)
                    {
                        PdfPreview.Source = new Uri(vm.GeneratedPdfPath);
                    }
                };

                // 초기화 완료 후 이미 생성된 PDF가 있으면 바로 표시
                if (vm.GeneratedPdfPath != null)
                    PdfPreview.Source = new Uri(vm.GeneratedPdfPath);
            }
        }
    }
}