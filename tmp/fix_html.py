content = open('sinterkin_survey_raw.json', encoding='utf-8').read()
start = content.index('"html":"') + len('"html":"')
end = content.rindex('"}')
html_escaped = content[start:end]
html = html_escaped.replace('\\r\\n', '\n').replace('\\n', '\n')
final = '<!DOCTYPE html>\n<html><head><meta charset="utf-8"></head><body>\n' + html + '\n</body></html>'
with open('sinterkin-naming-2026-08-05.html', 'w', encoding='utf-8') as f:
    f.write(final)
print(len(final))
print(final[:200])
print('---')
print(final[-300:])
