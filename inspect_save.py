import os, glob

save_dir = os.path.expandvars(r'%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves')
save_file = glob.glob(os.path.join(save_dir, '*dead*meat*.rws'))[0]

with open(save_file, 'r', encoding='utf-8', errors='ignore') as f:
    in_all = False
    in_li = False
    current = []
    for line in f:
        if '<allFactions>' in line:
            in_all = True
            continue
        if '</allFactions>' in line:
            break
        if in_all:
            if '<li>' in line:
                in_li = True
                current = []
            if in_li:
                current.append(line.strip())
                if '</li>' in line:
                    in_li = False
                    txt = '\n'.join(current)
                    # print name and def
                    name = ''
                    fdef = ''
                    loadid = ''
                    for l in current:
                        if '<name>' in l: name = l
                        if '<def>' in l: fdef = l
                        if '<loadID>' in l: loadid = l
                    if any(k in txt for k in ['Faction_154', 'Faction_179', '154', '179', 'PlayerColony', 'Miho']):
                        print(f'FACTION: {fdef} | {name} | {loadid}')
