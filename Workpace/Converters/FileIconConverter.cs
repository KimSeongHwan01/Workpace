using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Workpace.Converters
{
    // Windows Shell API를 통해 실제 시스템 파일 아이콘을 가져오는 Converter
    // 파일이 실제로 존재하지 않아도 확장자만으로 아이콘을 가져올 수 있음
    public class FileIconConverter : IValueConverter
    {
        // Windows Shell API 함수 선언
        // shell32.dll에서 파일 정보(아이콘 포함)를 가져오는 함수
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,          // 파일 경로 또는 확장자
            uint dwFileAttributes,   // 파일 속성
            ref SHFILEINFO psfi,     // 결과를 담을 구조체
            uint cbSizeFileInfo,     // 구조체 크기
            uint uFlags);            // 가져올 정보 종류 플래그

        // 아이콘 핸들을 해제하는 함수 — 메모리 누수 방지
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // SHGetFileInfo가 결과를 담는 구조체
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;       // 아이콘 핸들
            public int iIcon;          // 아이콘 인덱스
            public uint dwAttributes;  // 파일 속성
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName; // 표시 이름
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;    // 파일 형식 이름
        }

        // 플래그 상수
        private const uint SHGFI_ICON = 0x100;            // 아이콘 가져오기
        private const uint SHGFI_SMALLICON = 0x1;         // 작은 아이콘 (16x16)
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10; // 파일이 없어도 확장자만으로 아이콘 가져오기
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;   // 일반 파일 속성

        // 아이콘 캐시 — 같은 확장자는 한 번만 로드
        private static readonly Dictionary<string, ImageSource> _cache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fileName = value?.ToString() ?? "";
            if (string.IsNullOrEmpty(fileName)) return GetDefaultIcon();

            // 확장자 추출 — 캐시 키로 사용
            var ext = System.IO.Path.GetExtension(fileName).ToLower();
            if (string.IsNullOrEmpty(ext)) ext = fileName;

            // 캐시에 있으면 바로 반환 — 성능 최적화
            if (_cache.TryGetValue(ext, out var cached)) return cached;

            var shinfo = new SHFILEINFO();
            var result = SHGetFileInfo(
                ext,                         // 확장자만 넘겨도 아이콘 가져올 수 있음
                FILE_ATTRIBUTE_NORMAL,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            if (result == IntPtr.Zero) return GetDefaultIcon();

            try
            {
                // Win32 아이콘 핸들 → WPF ImageSource로 변환
                var icon = Icon.FromHandle(shinfo.hIcon);
                var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                imageSource.Freeze(); // UI 스레드 외에서도 안전하게 사용
                _cache[ext] = imageSource;
                return imageSource;
            }
            finally
            {
                // 아이콘 핸들 반드시 해제 — 안 하면 GDI 리소스 누수
                DestroyIcon(shinfo.hIcon);
            }
        }

        // 아이콘을 못 가져왔을 때 기본 이미지 반환
        private static ImageSource GetDefaultIcon()
        {
            if (_cache.TryGetValue("_default", out var cached)) return cached;

            var shinfo = new SHFILEINFO();
            SHGetFileInfo(".txt", FILE_ATTRIBUTE_NORMAL, ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            if (shinfo.hIcon == IntPtr.Zero) return null!;

            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            imageSource.Freeze();
            DestroyIcon(shinfo.hIcon);

            _cache["_default"] = imageSource;
            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}