import json
import sys
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8787
MAX_ITEMS = 10
MAX_USERS = 4

items = []
users = []

PAGE = """<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Mock sender</title>
<style>body{font-family:sans-serif;max-width:480px;margin:24px auto;padding:0 16px}
input,button{font-size:18px;padding:10px;width:100%;box-sizing:border-box;margin-top:8px}</style>
<h3>Mock sender (max 4 users)</h3>
<input id="user" placeholder="ชื่อผู้ใช้ / user name">
<input id="text" placeholder="ข้อความ / message">
<button onclick="send()">ส่ง / Send</button>
<p id="msg"></p>
<script>
async function send(){
  const r = await fetch('/api/send',{method:'POST',headers:{'Content-Type':'application/json'},
    body:JSON.stringify({user:user.value,text:text.value})});
  const j = await r.json();
  msg.textContent = r.ok ? 'sent: ' + j._id : j.error;
  if (r.ok) text.value = '';
}
</script>"""


class Handler(BaseHTTPRequestHandler):
    def _send(self, code, body, content_type="application/json; charset=utf-8"):
        data = body.encode("utf-8") if isinstance(body, str) else body
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()
        self.wfile.write(data)

    def do_OPTIONS(self):
        self._send(204, "")

    def do_GET(self):
        if self.path.split("?")[0] == "/api/unity.get":
            payload = [{"_id": i["_id"], "text": i["text"]} for i in items[-MAX_ITEMS:]]
            self._send(200, json.dumps({"items": payload}, ensure_ascii=False))
        elif self.path == "/":
            self._send(200, PAGE, "text/html; charset=utf-8")
        else:
            self._send(404, json.dumps({"error": "not found"}))

    def do_POST(self):
        if self.path != "/api/send":
            self._send(404, json.dumps({"error": "not found"}))
            return

        length = int(self.headers.get("Content-Length", 0))
        try:
            body = json.loads(self.rfile.read(length) or b"{}")
        except json.JSONDecodeError:
            self._send(400, json.dumps({"error": "invalid json"}))
            return

        user = str(body.get("user", "")).strip() or "anonymous"
        text = str(body.get("text", "")).strip()
        if not text:
            self._send(400, json.dumps({"error": "empty text"}))
            return

        if user not in users:
            if len(users) >= MAX_USERS:
                self._send(403, json.dumps({"error": "max 4 users reached"}, ensure_ascii=False))
                return
            users.append(user)

        item = {"_id": uuid.uuid4().hex[:12], "user": user, "text": text, "time": time.time()}
        items.append(item)
        del items[:-MAX_ITEMS * 5]
        self._send(200, json.dumps({"_id": item["_id"]}))


if __name__ == "__main__":
    print(f"Mock API on http://localhost:{PORT}  GET /api/unity.get  POST /api/send  form: /")
    ThreadingHTTPServer(("0.0.0.0", PORT), Handler).serve_forever()
