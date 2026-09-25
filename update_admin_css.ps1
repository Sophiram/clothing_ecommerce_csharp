$cssPath = 'D:/AEU-YEAR4/ECOMMERCE/ASP/WebApplication_ClothingEcommerce/WebApplication_ClothingEcommerce/wwwroot/css/admin.css'
$css = Get-Content $cssPath -Raw

$newRoot = @'
:root {
    /* Minimalist Fashion Palette */
    --google-blue: #000000;
    --google-blue-dark: #111111;
    --google-red: #D32F2F;
    --google-red-dark: #B71C1C;
    --google-green: #2E7D32;
    --google-green-dark: #1B5E20;
    --google-yellow: #F57F17;
    --google-yellow-dark: #E65100;
    
    --admin-bg: #F5F5F5;
    --admin-sidebar: #FFFFFF;
    --admin-sidebar-hover: #F2F2F2;
    --admin-sidebar-text: #666666;
    --admin-sidebar-active: #000000;
    --admin-text: #111111;
    --admin-muted: #888888;
    --admin-border: #E5E5E5;
    --admin-border-dark: #CCCCCC;
    --admin-card: #FFFFFF;
    
    --success: #2E7D32;
    --danger: #D32F2F;
    --warning: #F57F17;
    
    --sidebar-width: 260px;
    --sidebar-collapsed-width: 78px;
    --navbar-height: 72px;
    
    --radius-xs: 6px;
    --radius-sm: 8px;
    --radius-md: 12px;
    --radius-lg: 16px;
    
    --shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.05);
    --shadow-md: 0 4px 12px rgba(0, 0, 0, 0.05);
    --transition: all .25s cubic-bezier(0.4, 0, 0.2, 1);
}
'@

$css = $css -replace '(?s):root\s*\{.*?\n\}', $newRoot
$css = $css -replace 'background: linear-gradient\(180deg, var\(--admin-sidebar\), #1a1e29 80%\);', 'background: var(--admin-sidebar); border-right: 1px solid var(--admin-border);'
$css = $css -replace 'color: white;', 'color: var(--admin-text);'
$css = $css -replace 'background: linear-gradient\(90deg, var\(--google-blue\), var\(--google-blue-dark\)\);', 'background: var(--admin-sidebar-hover);'
$css = $css -replace 'box-shadow: 0 6px 16px rgba\(66, 133, 244, \.35\);', 'box-shadow: none;'

Set-Content $cssPath -Value $css
