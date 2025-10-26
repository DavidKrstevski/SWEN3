import React, { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";

const API_URL = "http://localhost:8080/api/documents";

function DocumentDetail() {
  const { id } = useParams();
  const [doc, setDoc] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadDoc() {
      try {
        const res = await fetch(`${API_URL}/${id}`);
        if (!res.ok) throw new Error("Document not found");
        const data = await res.json();
        setDoc(data);
      } catch (err) {
        setError(err.message);
      }
    }
    loadDoc();
  }, [id]);

  return (
    <div className="container py-5">
      <h1 className="text-center mb-4">Document Details</h1>
      {error && <div className="alert alert-danger">{error}</div>}
      {doc && (
        <div className="card p-4 shadow-sm">
          <h5>{doc.fileName}</h5>
          <p><strong>Size:</strong> {doc.size} bytes</p>
          <p><strong>Uploaded:</strong> {new Date(doc.uploadDate).toLocaleString()}</p>
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
