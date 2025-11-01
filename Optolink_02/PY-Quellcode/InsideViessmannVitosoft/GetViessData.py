import os
from xml.etree import ElementTree as ET

DATAPATH = "D:/Viessmann/Viessmann Vitosoft 300 SID1/ServiceTool/MobileClient/Config/"

def load_textresources(lang="de"):
    text_dict = {}
    tree = ET.parse(os.path.join(DATAPATH, f"Textresource_{lang}.xml"))
    root = tree.getroot()
    for child in root.iter():
        if 'Label' in child.attrib and 'Value' in child.attrib:
            text_dict[child.attrib['Label']] = child.attrib['Value']
    return text_dict

def get_all_datapoints():
    textresource = load_textresources("de")
    datapoints = []
    tree = ET.parse(os.path.join(DATAPATH, "ecnDataPointType.xml"))
    root = tree.getroot()
    for node in root:
        dpid = node.findtext('ID')
        name = node.findtext('Name')
        address = node.findtext('Identification')
        if name and name.startswith('@@'):
            name_key = name[2:]
            name = textresource.get(name_key, name_key) # Übersetze oder nutze Key
        datapoints.append({'id': dpid, 'name': name, 'ident': address})
    return datapoints

def get_events_for_datapoint(datapoint_id):
    # Aus DPDefinitions.xml alle Events zu diesem Datapoint suchen
    # (Hier ist nur ein Dummy, du hast schon gute Funktionen, die du hier verwenden kannst!)
    return [{'event_id': '329', 'event_name': 'Außentemperatur'}, ... ]

def show_datapoint_list(datapoints):
    for idx, dp in enumerate(datapoints, 1):
        print(f"{idx}: {dp['name']} ({dp['ident']})")
    print()

def main():
    # 1. Alle Geräte anzeigen
    datapoints = get_all_datapoints()
    while True:
        print("\nWelche Heizungseinheit möchtest du durchsuchen?")
        show_datapoint_list(datapoints)
        try:
            auswahl = int(input("Bitte Nummer eingeben (oder 0 zum Beenden): "))
        except ValueError:
            continue
        if auswahl == 0:
            break
        if auswahl < 1 or auswahl > len(datapoints):
            print("Ungültige Auswahl!")
            continue

        gew_dp = datapoints[auswahl - 1]
        print(f"\nDu hast gewählt: {gew_dp['name']}")

        # 2. Events für diesen Datapoint ausgeben
        events = get_events_for_datapoint(gew_dp['id'])
        for idx, event in enumerate(events, 1):
            print(f"  {idx}: {event['event_name']} (ID: {event['event_id']})")
        print()

        # 3. Export oder Neue Auswahl
        action = input("Export als (C)SV, (X)ML, (N)eue Auswahl oder (B)eenden? ").strip().lower()
        if action == 'c':
            # Hier export_datapoints_to_csv() oder export_events_to_csv() aufrufen
            print("Exportiere CSV...")
        elif action == 'x':
            # Hier export_datapoints_to_xml() oder export_events_to_xml() aufrufen
            print("Exportiere XML...")
        elif action == 'b':
            break
        # sonst Schleife für neue Auswahl

if __name__ == "__main__":
    dplist = get_all_datapoints()
    show_datapoint_list(dplist)
