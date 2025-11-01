-- Teste die ersten 30 Events mit ihren Gruppen und Kategorien
SELECT TOP 30
    et.Id AS EventId,
    et.[Name] AS EventName,
    etg.[Name] AS GruppeName,
    etg.ParentId AS GruppeParentId,
    etgParent.[Name] AS KategorieName
FROM ecnEventType et
LEFT JOIN ecnDataPointTypeEventTypeLink dpe ON et.Id = dpe.EventTypeId
LEFT JOIN ecnDatapointType dp ON dp.Id = dpe.DataPointTypeId
LEFT JOIN (
    SELECT EventTypeId, MIN(EventTypeGroupId) AS FirstGroupId
    FROM ecnEventTypeEventTypeGroupLink 
    GROUP BY EventTypeId
) fgpe ON fgpe.EventTypeId = et.Id
LEFT JOIN ecnEventTypeGroup etg ON etg.Id = fgpe.FirstGroupId
LEFT JOIN ecnEventTypeGroup etgParent ON etgParent.Id = etg.ParentId
WHERE dp.Address = 'VScotHO1_20'
  AND etg.[Name] IS NOT NULL
ORDER BY et.Id;

-- Zeige Statistik über Kategorie-Werte
SELECT 
    'Gesamt' AS Typ,
    COUNT(*) AS Anzahl
FROM ecnEventType et
LEFT JOIN ecnDataPointTypeEventTypeLink dpe ON et.Id = dpe.EventTypeId
LEFT JOIN ecnDatapointType dp ON dp.Id = dpe.DataPointTypeId
WHERE dp.Address = 'VScotHO1_20'

UNION ALL

SELECT 
    'Mit Gruppe' AS Typ,
    COUNT(*) AS Anzahl
FROM ecnEventType et
LEFT JOIN ecnDataPointTypeEventTypeLink dpe ON et.Id = dpe.EventTypeId
LEFT JOIN ecnDatapointType dp ON dp.Id = dpe.DataPointTypeId
LEFT JOIN (SELECT EventTypeId, MIN(EventTypeGroupId) AS FirstGroupId FROM ecnEventTypeEventTypeGroupLink GROUP BY EventTypeId) fgpe ON fgpe.EventTypeId = et.Id
LEFT JOIN ecnEventTypeGroup etg ON etg.Id = fgpe.FirstGroupId
WHERE dp.Address = 'VScotHO1_20'
  AND etg.[Name] IS NOT NULL

UNION ALL

SELECT 
 'Mit Kategorie (ParentId vorhanden)' AS Typ,
    COUNT(*) AS Anzahl
FROM ecnEventType et
LEFT JOIN ecnDataPointTypeEventTypeLink dpe ON et.Id = dpe.EventTypeId
LEFT JOIN ecnDatapointType dp ON dp.Id = dpe.DataPointTypeId
LEFT JOIN (SELECT EventTypeId, MIN(EventTypeGroupId) AS FirstGroupId FROM ecnEventTypeEventTypeGroupLink GROUP BY EventTypeId) fgpe ON fgpe.EventTypeId = et.Id
LEFT JOIN ecnEventTypeGroup etg ON etg.Id = fgpe.FirstGroupId
LEFT JOIN ecnEventTypeGroup etgParent ON etgParent.Id = etg.ParentId
WHERE dp.Address = 'VScotHO1_20'
  AND etg.[Name] IS NOT NULL
  AND etg.ParentId IS NOT NULL
  AND etgParent.[Name] IS NOT NULL;
