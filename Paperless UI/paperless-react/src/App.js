import React, { useEffect, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

const API_URL = "http://localhost:8080/api/documents";

function App() {
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
    } catch (err) {
      console.error(err);
      setMessage("Error loading documents");
    }
  }

  async function addDocument(e) {
    e.preventDefault();
    if (!fileName || !size) {
      setMessage("Please provide both name and size");
      return;
    }

    try {
      const res = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName, size: parseInt(size, 10) }),
      });
      if (!res.ok) throw new Error("Failed to add document");
      setFileName("");
      setSize("");
      setMessage("Document added successfully!");
      loadDocuments();
    } catch (err) {
      console.error(err);
      setMessage("Error adding document");
    }
  }

  async function deleteDocument(id) {
    if (!window.confirm("Delete this document?")) return;
    try {
      const res = await fetch(`${API_URL}/${id}`, { method: "DELETE" });
      if (res.ok) loadDocuments();
      else setMessage("Failed to delete document");
    } catch (err) {
      console.error(err);
      setMessage("Error deleting document");
    }
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
              <div>
                <strong>{d.fileName}</strong>{" "}
                <small>({d.size} bytes)</small>
              </div>
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
          <div className="mb-3">
            <input
              type="text"
              className="form-control"
              placeholder="Document name"
              value={fileName}
              onChange={(e) => setFileName(e.target.value)}
              required
            />
          </div>
          <div className="mb-3">
            <input
              type="number"
              className="form-control"
              placeholder="Size in bytes"
              value={size}
              onChange={(e) => setSize(e.target.value)}
              required
            />
          </div>
          <button type="submit" className="btn btn-success">
            Add Document
          </button>
        </form>
      </div>
    </div>
  );
}

export default App;
