try
{
    $XsltSettings = New-Object System.Xml.Xsl.XsltSettings($true, $false); 
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath.Load("missinggames.xslt",$XsltSettings, $null)
    $XslPath.Transform("nswdb.xml", "missinggames.xml")
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath.Load("games.xslt")
    $XslPath.Transform("SBM_Local.xml", "SBM_Local.html")
    $XslPath.Transform("SBM_NSP_Local.xml", "SBM_NSP_Local.html")
    $XslPath.Transform("missinggames.xml", "missing.html")
    
    Write-Host "Done !"
}
catch
{
    Write-Host $_.Exception -ForegroundColor Red
}