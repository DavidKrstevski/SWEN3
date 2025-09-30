const API_URL = "http://localhost:8080/api/documents/";

// Load all documents
async function loadDocuments() {
  const res = await fetch(API_URL);
  const docs = await res.json();
  const list = document.getElementById("docs");
  list.innerHTML = "";

  docs.forEach(d => {
    const li = document.createElement("li");
    li.className = "list-group-item d-flex justify-content-between align-items-center";
    li.innerHTML = `
      <a href="detail.html?id=${d.id}" class="text-decoration-none">${d.fileName}</a>
      <div>
        <span class="badge bg-secondary rounded-pill me-2">${d.size} bytes</span>
        <button class="btn btn-sm btn-danger delete-btn" data-id="${d.id}">Delete</button>
      </div>
    `;
    list.appendChild(li);
  });

  // Attach delete handlers
  document.querySelectorAll(".delete-btn").forEach(btn => {
    btn.addEventListener("click", async () => {
      const docId = btn.getAttribute("data-id");
      if (!confirm("Are you sure you want to delete this document?")) return;

      try {
        const res = await fetch(`${API_URL}${docId}`, { method: "DELETE" });
        if (res.ok) {
          loadDocuments(); // Refresh list
        } else {
          const errData = await res.json();
          alert(`Failed to delete document: ${errData.message || res.statusText}`);
        }
      } catch (err) {
        console.error(err);
        alert(`Error: ${err.message}`);
      }
    });
  });
}

// Load a single document
async function loadDocument(id) {
  const res = await fetch(`${API_URL}${id}`);
  const docContainer = document.getElementById("doc");

  if (!res.ok) {
    docContainer.innerHTML = `<div class="alert alert-danger">Document not found</div>`;
    return;
  }

  const d = await res.json();
  docContainer.innerHTML = `
    <h5 class="card-title mb-3">${d.fileName}</h5>
    <p class="card-text"><strong>Size:</strong> ${d.size} bytes</p>
    <p class="card-text"><strong>Uploaded:</strong> ${new Date(d.uploadDate).toLocaleString()}</p>
  `;
}

// Handle document "upload" (name + size)
document.addEventListener("DOMContentLoaded", () => {
  const uploadForm = document.getElementById("uploadForm");
  if (uploadForm) {
    uploadForm.addEventListener("submit", async (e) => {
      e.preventDefault();

      const name = document.getElementById("docName").value.trim();
      const size = parseInt(document.getElementById("docSize").value, 10);
      const messageBox = document.getElementById("uploadMessage");

      if (!name || isNaN(size) || size < 0) {
        messageBox.innerHTML = `<div class="alert alert-warning">Please enter a valid name and size</div>`;
        return;
      }

      const payload = { fileName: name, size: size };

      try {
        const res = await fetch(API_URL, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });

        if (res.ok) {
          messageBox.innerHTML = `<div class="alert alert-success">Document added successfully!</div>`;
          document.getElementById("docName").value = "";
          document.getElementById("docSize").value = "";
          loadDocuments();
        } else {
          const errData = await res.json();
          messageBox.innerHTML = `<div class="alert alert-danger">Error: ${errData.message || 'Failed to add document'}</div>`;
        }
      } catch (err) {
        console.error(err);
        messageBox.innerHTML = `<div class="alert alert-danger">Error: ${err.message}</div>`;
      }
    });
  }
});
