<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes"/>
<xsl:param name="xci" select="'SBM_Local.xml'"/>
<xsl:param name="nsp" select="'SBM_NSP_Local.xml'"/>
  
<xsl:template match="/releases">

  <xsl:variable name="docXCI" select="document($xci)"/>
  <xsl:variable name="docNSP" select="document($nsp)"/>

  <xsl:element name="Games">
    <xsl:for-each select="$docXCI/Games/Game">
      <xsl:copy>
        <xsl:attribute name="type">xci</xsl:attribute>
        <xsl:copy-of select="@*"/>
        <xsl:copy-of select="*"/>
      </xsl:copy>
    </xsl:for-each>
    <xsl:for-each select="$docNSP/Games/Game">
      <xsl:copy>
        <xsl:attribute name="type">nsp</xsl:attribute>
        <xsl:copy-of select="@*"/>
        <xsl:copy-of select="*"/>
      </xsl:copy>
    </xsl:for-each>
  </xsl:element>

</xsl:template>
</xsl:stylesheet>
