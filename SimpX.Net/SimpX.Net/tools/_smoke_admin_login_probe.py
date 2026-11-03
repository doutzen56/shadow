# -*- coding: utf-8 -*-
import json
import urllib.parse
import urllib.request
import http.cookiejar

base = "http://127.0.0.1:5005"


def try_login(extra_headers):
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    body = urllib.parse.urlencode(
        {"UserName": "tzen001", "Password": "a123456"}
    ).encode()
    req = urllib.request.Request(base + "/admin/account/login", data=body, method="POST")
    req.add_header("Content-Type", "application/x-www-form-urlencoded")
    for k, v in extra_headers.items():
        req.add_header(k, v)
    try:
        with op.open(req, timeout=30) as r:
            raw = r.read().decode("utf-8", "replace")
            code = r.status
            headers = dict(r.headers)
    except Exception as e:
        raw = e.read().decode("utf-8", "replace") if hasattr(e, "read") else str(e)
        code = getattr(e, "code", 0)
        headers = {}
    print("HDR", extra_headers, "=>", code, raw[:200].replace("\n", " "))
    token_keys = {k: headers[k] for k in headers if "token" in k.lower() or "set-cookie" in k.lower()}
    if token_keys:
        print("  RESP", token_keys)
    try:
        obj = json.loads(raw)
        if obj.get("success") in (1, True, "1"):
            return True, cj, headers, raw
    except Exception:
        pass
    return False, cj, headers, raw


candidates = [
    {},
    {"X-Site-Id": "1002"},
    {"X-SiteID": "1002"},
    {"SiteID": "1002"},
    {"X-Forwarded-Host": "localhost"},
    {"X-Forwarded-HostX": "localhost"},
]

ok = False
for h in candidates:
    ok, cj, headers, raw = try_login(h)
    if ok:
        print("LOGIN OK")
        break

if not ok:
    raise SystemExit(1)

# smoke authenticated
op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
token = headers.get("TOKEN") or headers.get("Token") or headers.get("token")


def post(path, data=None):
    body = urllib.parse.urlencode(data or {}).encode()
    req = urllib.request.Request(base + path, data=body, method="POST")
    req.add_header("Content-Type", "application/x-www-form-urlencoded")
    if token:
        req.add_header("TOKEN", token)
        req.add_header("Authorization", "Bearer " + token)
    with op.open(req, timeout=30) as r:
        return r.status, r.read().decode("utf-8", "replace")


for p, data in [
    ("/admin/info/init", {}),
    ("/admin/user/agent/info/agentlist", {}),
    ("/admin/user/agent/usragent/check", {"Status": "1", "Users": "fstest08"}),
    ("/admin/exchange/rate/getcurrency", {}),
    ("/admin/exchange/rate/getmode", {}),
    ("/admin/exchange/rate/getfee", {}),
]:
    code, raw = post(p, data)
    try:
        obj = json.loads(raw)
        print(p, code, "success=", obj.get("success"), "msg=", str(obj.get("msg"))[:80])
    except Exception:
        print(p, code, raw[:160])
