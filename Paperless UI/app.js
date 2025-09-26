const API_URL = "http://localhost:8080/api/documents/";

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
      <span class="badge bg-secondary rounded-pill">${d.size} bytes</span>
    `;
    list.appendChild(li);
  });
}

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
