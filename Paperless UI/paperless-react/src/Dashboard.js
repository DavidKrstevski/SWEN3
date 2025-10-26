import React, { useEffect, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

const API_URL = "http://localhost:8080/api/documents";

function Dashboard() {
  const [docs, setDocs] = useState([]);
  const [fileName, setFileName] = useState("");
  const [size, setSize] = useState("");
  const [message, setMessage] = useState("");

  async function loadDocuments() {
    try {
      const res = await fetch(API_URL);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setDocs(data);
    } catch {
      setMessage("Error loading documents");
    }
  }

  async function addDocument(e) {
    e.preventDefault();
    try {
      const res = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName, size: parseInt(size, 10) }),
      });
      if (!res.ok) throw new Error();
      setFileName("");
      setSize("");
      setMessage("Document added successfully!");
      loadDocuments();
    } catch {
      setMessage("Error adding document");
    }
  }

  async function deleteDocument(id) {
    if (!window.confirm("Delete this document?")) return;
    await fetch(`${API_URL}/${id}`, { method: "DELETE" });
    loadDocuments();
  }

  useEffect(() => {
    loadDocuments();
  }, []);

  return (
    <div className="container py-5">
      <h1 className="text-center mb-4">Document Dashboard</h1>
      {message && <div className="alert alert-info">{message}</div>}

      <ul className="list-group mb-4">
        {docs.length === 0 ? (
          <li className="list-group-item">No documents found</li>
        ) : (
          docs.map((d) => (
            <li
              key={d.id}
              className="list-group-item d-flex justify-content-between align-items-center"
            >
              <a
                href={`/documents/${d.id}`}
                className="text-decoration-none"
              >
                <strong>{d.fileName}</strong> ({d.size} bytes)
              </a>
              <button
                className="btn btn-sm btn-danger"
                onClick={() => deleteDocument(d.id)}
              >
                Delete
              </button>
            </li>
          ))
        )}
      </ul>

      <div className="card p-4 shadow-sm">
        <h5 className="mb-3">Add a New Document</h5>
        <form onSubmit={addDocument}>
          <input
            className="form-control mb-2"
            placeholder="Document name"
            value={fileName}
            onChange={(e) => setFileName(e.target.value)}
            required
          />
          <input
            className="form-control mb-2"
            type="number"
            placeholder="Size in bytes"
            value={size}
            onChange={(e) => setSize(e.target.value)}
            required
          />
          <button className="btn btn-success w-100">Add Document</button>
        </form>
      </div>
    </div>
  );
}

export default Dashboard;
