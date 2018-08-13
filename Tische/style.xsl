﻿<?xml version="1.0" encoding="utf-8" ?>
<!--
   This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
   Refer to README.md in the Tische distribution for licensing and
   distribution terms.
-->
<xsl:stylesheet version="1.1" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fo="http://www.w3.org/1999/XSL/Format" exclude-result-prefixes="fo">

  <!-- Create the output from the root element, <report>. -->
  <xsl:template match="report">
    <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
      <fo:layout-master-set>
        <fo:simple-page-master
          master-name="A4"
          page-height="29.7cm"
          page-width="21cm"
          margin-top="1cm"
          margin-bottom="1cm"
          margin-left="1cm"
          margin-right="1cm">
          <fo:region-body />
        </fo:simple-page-master>
      </fo:layout-master-set>

      <fo:page-sequence master-reference="A4">
        <fo:flow flow-name="xsl-region-body">
          <fo:block font-size="10pt">
            <fo:table table-layout="fixed" width="100%" border="solid" border-collapse="collapse">
              <!-- The column definitions, to generate <fo:table-column>s. -->
              <xsl:apply-templates select="/report/columns/column" />

              <!-- Printed column headers -->
              <fo:table-header>
                <xsl:apply-templates select="header" />
              </fo:table-header>

              <!-- Table data rows -->
              <fo:table-body>
                <xsl:apply-templates select="row" />
              </fo:table-body>
            </fo:table>
          </fo:block>
        </fo:flow>
      </fo:page-sequence>
    </fo:root>
  </xsl:template>

  <!-- Column style definitions -->
  <xsl:template match="/report/columns/column">
    <fo:table-column>
      <xsl:attribute name="column-width">
        <xsl:choose>
          <xsl:when test="@width">
            <xsl:value-of select="@width" />
          </xsl:when>
          <xsl:otherwise>
            auto
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
    </fo:table-column>
  </xsl:template>

  <!-- Style applied for the header row. -->
  <xsl:template match="header">
    <fo:table-row>
      <xsl:for-each select="i">
        <fo:table-cell
          border="solid"
          padding-left="1mm"
          padding-right="1mm"
          padding-top="0.5mm"
          padding-bottom="0.5mm">

          <fo:block font-weight="bold">
            <xsl:value-of select="text()" />
          </fo:block>
        </fo:table-cell>
      </xsl:for-each>
    </fo:table-row>
  </xsl:template>

  <!-- Style applied for each row. -->
  <xsl:template match="row">
    <fo:table-row>
      <xsl:for-each select="i">
        <fo:table-cell
          border="solid"
          padding-left="1mm"
          padding-right="1mm"
          padding-top="0.5mm"
          padding-bottom="0.5mm">

          <fo:block>
            <xsl:value-of select="text()" />
          </fo:block>
        </fo:table-cell>
      </xsl:for-each>
    </fo:table-row>
  </xsl:template>
</xsl:stylesheet>
