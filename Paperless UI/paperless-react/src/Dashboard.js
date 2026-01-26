import React, { useEffect, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { Link } from "react-router-dom";

const API_URL = "http://localhost:8080/api/documents";

function Dashboard() {
  const [docs, setDocs] = useState([]);
  const [file, setFile] = useState(null);
  const [message, setMessage] = useState("");
  const [searchTerm, setSearchTerm] = useState("");

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

  async function uploadDocument(e) {
    e.preventDefault();
    if (!file) {
      setMessage("Please select a file first.");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    try {
      const res = await fetch(API_URL, {
        method: "POST",
        body: formData,
      });

      if (!res.ok) throw new Error();
      const newDoc = await res.json();
      setMessage("File uploaded successfully!");
      setFile(null);
      setDocs((prev) => [...prev, newDoc]);
    } catch {
      setMessage("Error uploading file");
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

  const getBorderColor = (riskColor) => {
    const rc = (riskColor || "").toString().trim().toLowerCase();
    if (rc === "green") return "green";
    if (rc === "yellow") return "gold";
    if (rc === "orange") return "orange";
    if (rc === "red") return "red";
    return "black";
  };

  const filteredDocs = docs.filter((d) =>
    (d.fileName || "").toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="container py-5">
      <h1 className="text-center mb-4">Document Dashboard</h1>
      {message && <div className="alert alert-info">{message}</div>}

      {}
      <input
        type="text"
        className="form-control mb-3"
        placeholder="Search by document name..."
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
      />

      <ul className="list-group mb-4">
        {filteredDocs.length === 0 ? (
          <li className="list-group-item">
            {docs.length === 0 ? "No documents found" : "No matching documents"}
          </li>
        ) : (
          filteredDocs.map((d) => (
            <li
              key={d.id}
              className="list-group-item d-flex justify-content-between align-items-center"
              style={{
                border: "2px solid",
                borderColor: getBorderColor(d.riskColor),
              }}
            >
              <Link to={`/documents/${d.id}`} className="text-decoration-none">
                <strong>{d.fileName}</strong> ({d.size} bytes)
              </Link>

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
        <h5 className="mb-3">Upload a PDF Document</h5>
        <form onSubmit={uploadDocument}>
          <input
            type="file"
            accept="application/pdf"
            className="form-control mb-2"
            onChange={(e) => setFile(e.target.files[0])}
            required
          />
          <button className="btn btn-success w-100">Upload PDF</button>
        </form>
      </div>
    </div>
  );
}

export default Dashboard;
