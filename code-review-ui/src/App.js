import React, { useState } from "react";
import ReactDiffViewer from "react-diff-viewer-continued";

function App() {
  const [beforeText, setBeforeText] = useState("");
  const [afterText, setAfterText] = useState("");
  const [review, setReview] = useState("");
  const [loading, setLoading] = useState(false);
  const [beforeFileName, setBeforeFileName] = useState("");
  const [afterFileName, setAfterFileName] = useState("");

  // ================= FILE UPLOAD =================
  const handleFileUpload = (event, type) => {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();

    reader.onload = (e) => {
      if (type === "before") {
        setBeforeText(e.target.result);
        setBeforeFileName(file.name);
      } else {
        setAfterText(e.target.result);
        setAfterFileName(file.name);
      }
    };

    reader.readAsText(file);
  };

  // ================= API CALL =================
  const handleSubmit = async () => {
    if (!beforeText || !afterText) {
      alert("Please provide both code inputs");
      return;
    }

    setLoading(true);
    setReview("");

    try {
      const response = await fetch(`${process.env.REACT_APP_API_URL}/review`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          beforeCode: beforeText,
          afterCode: afterText,
        }),
      });

      const data = await response.json();
      setReview(data.review);
    } catch (err) {
      setReview("Error connecting to API");
    }

    setLoading(false);
  };

  return (
    <div style={{
      background: "#0d1117",
      color: "#c9d1d9",
      minHeight: "100vh",
      padding: "20px",
      fontFamily: "Arial"
    }}>
      <h1 style={{ color: "#58a6ff" }}>AI Code Review Tool</h1>

      {/* FILE UPLOAD */}
      <div style={{ marginBottom: "20px" }}>
        <div>
          <label>Upload BEFORE file: </label>
          <input type="file" accept=".cs" onChange={(e) => handleFileUpload(e, "before")} />
          {beforeFileName && <span> ({beforeFileName})</span>}
        </div>

        <br />

        <div>
          <label>Upload AFTER file: </label>
          <input type="file" accept=".cs" onChange={(e) => handleFileUpload(e, "after")} />
          {afterFileName && <span> ({afterFileName})</span>}
        </div>
      </div>

      {/* TEXT INPUT */}
      <div style={{ display: "flex", gap: "20px", marginBottom: "20px" }}>
        <textarea
          placeholder="Paste BEFORE code..."
          value={beforeText}
          onChange={(e) => setBeforeText(e.target.value)}
          style={{
            flex: 1,
            height: "200px",
            background: "#161b22",
            color: "#c9d1d9",
            border: "1px solid #30363d",
            padding: "10px"
          }}
        />

        <textarea
          placeholder="Paste AFTER code..."
          value={afterText}
          onChange={(e) => setAfterText(e.target.value)}
          style={{
            flex: 1,
            height: "200px",
            background: "#161b22",
            color: "#c9d1d9",
            border: "1px solid #30363d",
            padding: "10px"
          }}
        />
      </div>

      {/* BUTTON */}
      <button
        onClick={handleSubmit}
        style={{
          padding: "10px 20px",
          background: "#238636",
          color: "white",
          border: "none",
          cursor: "pointer",
          borderRadius: "6px"
        }}
      >
        {loading ? "Reviewing..." : "Review Code"}
      </button>

      {/* DIFF VIEW */}
      {beforeText && afterText && (
        <div style={{ marginTop: "30px" }}>
          <h2>Code Differences</h2>
          <ReactDiffViewer
            oldValue={beforeText}
            newValue={afterText}
            splitView={true}
            useDarkTheme={true}
          />
        </div>
      )}

      {/* AI REVIEW OUTPUT */}
      {review && (
        <div style={{
          marginTop: "30px",
          background: "#161b22",
          padding: "20px",
          borderRadius: "8px",
          border: "1px solid #30363d"
        }}>
          <h2 style={{ color: "#58a6ff" }}>AI Review</h2>

          {review.split("\n").map((line, i) => (
            <div key={i} style={{ marginBottom: "5px" }}>
              • {line}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default App;