# -*- coding: utf-8 -*-
import json
import urllib.parse
import urllib.request
import http.cookiejar

base = "http://127.0.0.1:5005"
cj = http.cookiejar.CookieJar()
op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))


def post(path, data=None, token=None):
    body = urllib.parse.urlencode(data or {}).encode()
    req = urllib.request.Request(base + path, data=body, method="POST")
    req.add_header("Content-Type", "application/x-www-form-urlencoded")
    if token:
        req.add_header("TOKEN", token)
    with op.open(req, timeout=30) as r:
        return r.status, r.read().decode("utf-8", "replace"), dict(r.headers)


code, raw, headers = post("/admin/account/login", {"UserName": "tzen001", "Password": "a123456"})
obj = json.loads(raw)
assert obj.get("success") == 1, raw
token = headers.get("TOKEN") or headers.get("Token")
print("LOGIN", code, "success=1", "token=", bool(token))

cases = [
    ("/admin/info/init", {}),
    ("/admin/info/get", {}),
    ("/admin/user/agent/info/agentlist", {}),
    ("/admin/user/agent/usragent/list", {}),
    ("/admin/user/agent/usragent/check", {"Status": "1", "Users": "fstest08"}),
    ("/admin/user/agent/invite/list", {}),
    ("/admin/exchange/rate/getcurrency", {}),
    ("/admin/exchange/rate/getcurrencyinfo", {}),
    ("/admin/exchange/rate/getmode", {}),
    ("/admin/exchange/rate/getfee", {}),
    ("/admin/exchange/rate/list", {"Currency": "0"}),
]

ok = 0
for path, data in cases:
    code, raw, _ = post(path, data, token=token)
    obj = json.loads(raw)
    success = obj.get("success")
    msg = str(obj.get("msg") or "")[:60]
    info = obj.get("info")
    info_s = str(info)[:100].replace("\n", " ")
    print(f"{path} {code} success={success} msg={msg} info={info_s}")
    if success == 1:
        ok += 1

print(f"PASS {ok}/{len(cases)}")
