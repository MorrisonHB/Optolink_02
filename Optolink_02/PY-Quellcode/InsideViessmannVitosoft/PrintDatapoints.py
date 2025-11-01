from xml.etree import ElementTree as etree
import csv

DATAPATH = "C:/Users/Morri/Desktop/InsideViessmannVitosoft-main/XML-Dateien/"

def get_datapoint_display_lines():
    evnDataPointTypes = {}
    def parse_ecnDataPointType():
        for nodes in etree.parse(DATAPATH + "ecnDataPointType.xml").getroot():
            dataPointType = {}
            for cell in nodes:
                value = cell.text
                if cell.tag in ['Description','EventOptimisationExceptionList','EventOptimisation','Options','ErrorType']:
                    continue
                if cell.tag in ['ControllerType','ErrorType','EventOptimisation']:
                    try:
                        value = int(value)
                    except Exception:
                        pass
                dataPointType[cell.tag] = value
            ID = dataPointType.get('ID', '').strip()
            if not ID:
                continue
            del dataPointType['ID']
            evnDataPointTypes[ID] = dataPointType
    parse_ecnDataPointType()
    lines = []
    for ID,dataPointType in evnDataPointTypes.items():
        if 'Identification' not in dataPointType:
            continue
        if not dataPointType['Identification'].startswith('20'):
            continue
        if 'IdentificationExtension' in dataPointType and len(dataPointType['IdentificationExtension']) != 4:
            continue
        idStr = 'ecnsysDeviceIdent:' + dataPointType['Identification']
        if 'IdentificationExtension' in dataPointType:
            idStr += ' sysHardware/SoftwareIndexIdent:' + dataPointType['IdentificationExtension']
            if 'IdentificationExtensionTill' in dataPointType:
                idStr += '-' + dataPointType['IdentificationExtensionTill']
        if 'F0' in dataPointType:
            idStr += ' ecnsysDeviceIdentF0:' + dataPointType['F0']
            if 'F0Till' in dataPointType:
                idStr += '-' + dataPointType['F0Till']
        lines.append([idStr, ID])
    lines.sort()
    return lines

def export_display_lines_to_csv(lines, filename="datapoints_display_export.csv"):
    with open(filename, mode="w", encoding="utf-8", newline="") as csvfile:
        writer = csv.writer(csvfile, delimiter=';')
        writer.writerow(["DeviceIdent", "ShortName"])
        for row in lines:
            writer.writerow(row)
    print(f"Display-Lines wurden als CSV gespeichert: {filename}")

if __name__ == "__main__":
    lines = get_datapoint_display_lines()
    # Konsolenanzeige:
    for idStr, shortName in lines:
        print(f"{idStr} : {shortName}")
    # CSV-Export
    export_display_lines_to_csv(lines)
