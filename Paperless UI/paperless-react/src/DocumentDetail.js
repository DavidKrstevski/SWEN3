import React, { useEffect, useState, useCallback, useRef, useMemo } from "react";
import { useParams, Link } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";

const API_URL = "http://localhost:8080/api/documents";

function DocumentDetail() {
  const { id } = useParams();
  const [doc, setDoc] = useState(null);
  const [error, setError] = useState("");

  const [chatInput, setChatInput] = useState("");
  const [chatStatus, setChatStatus] = useState("");
  const [sending, setSending] = useState(false);
  const chatBottomRef = useRef(null);

  const loadDoc = useCallback(async () => {
    try {
      const res = await fetch(`${API_URL}/${id}`);
      if (!res.ok) throw new Error("Document not found");
      const data = await res.json();
      setDoc(data);
      setError("");
    } catch (err) {
      setError(err.message);
    }
  }, [id]);

  useEffect(() => {
    loadDoc();
  }, [loadDoc]);

  // split chat history by ---ENTER---
  const chatMessages = useMemo(() => {
    const raw = (doc?.chatHistory || "").trim();
    if (!raw) return [];
    return raw
      .split("---ENTER---")
      .map((m) => m.trim())
      .filter(Boolean);
  }, [doc?.chatHistory]);

  useEffect(() => {
    chatBottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [chatMessages.length, chatStatus]);

  async function sendChat(e) {
    e.preventDefault();
    const msg = chatInput.trim();
    if (!msg || sending) return;

    setSending(true);
    setChatStatus("");

    try {
      const res = await fetch(`${API_URL}/${id}/ask`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(msg),
      });

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(txt || `HTTP ${res.status}`);
      }

      setChatInput("");
      setChatStatus("Frage wurde gesendet (wird im Hintergrund verarbeitet).");

      setTimeout(() => loadDoc(), 1500);
    } catch (err) {
      setChatStatus(`Fehler: ${err.message}`);
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="container py-5">
      <h1 className="text-center mb-4">Document Details</h1>
      {error && <div className="alert alert-danger">{error}</div>}

      {doc && (
        <div className="card p-4 shadow-sm">
          <h5>{doc.fileName}</h5>
          <p>
            <strong>Size:</strong> {doc.size} bytes
          </p>
          <p>
            <strong>Uploaded:</strong> {new Date(doc.uploadDate).toLocaleString()}
          </p>

          <hr />

          <p>
            <strong>Summary:</strong>
          </p>
          <div className="bg-light p-3 rounded" style={{ whiteSpace: "pre-wrap" }}>
            {doc.summary || "No summary available."}
          </div>

          <p className="mt-3">
            <strong>Chat history:</strong>
          </p>

          <div
            className="bg-light p-3 rounded"
            style={{ maxHeight: 320, overflowY: "auto" }}
          >
            {chatMessages.length === 0 ? (
              <div style={{ whiteSpace: "pre-wrap" }}>No chat history available.</div>
            ) : (
              chatMessages.map((text, idx) => {
                const isAi = idx % 2 === 0;

                return (
                  <div
                    key={idx}
                    className={`d-flex mb-2 ${isAi ? "justify-content-start" : "justify-content-end"}`}
                  >
                    <div
                      className={`p-2 rounded`}
                      style={{
                        maxWidth: "80%",
                        whiteSpace: "pre-wrap",
                        background: isAi ? "#ffffff" : "#dbeafe",
                        border: "1px solid rgba(0,0,0,0.08)",
                      }}
                    >
                      <div className="small text-muted mb-1">
                        {isAi ? "Gemini" : "You"}
                      </div>
                      {text}
                    </div>
                  </div>
                );
              })
            )}
            <div ref={chatBottomRef} />
          </div>

          {/* Chat Input unten */}
          <div className="mt-3">
            {chatStatus && <div className="alert alert-info py-2">{chatStatus}</div>}

            <form onSubmit={sendChat} className="d-flex gap-2">
              <input
                type="text"
                className="form-control"
                placeholder="Schreib deine Frage zum Dokument…"
                value={chatInput}
                onChange={(e) => setChatInput(e.target.value)}
                disabled={sending}
              />
              <button className="btn btn-primary" disabled={sending || !chatInput.trim()}>
                {sending ? "Sending…" : "Send"}
              </button>
            </form>
          </div>
        </div>
      )}

      <div className="text-center mt-4">
        <Link to="/" className="btn btn-primary">
          Back to Dashboard
        </Link>
      </div>
    </div>
  );
}

export default DocumentDetail;