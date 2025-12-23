
import mermaid from "src/mermaid.esm.mjs";
import elkLayouts from "src/mermaid-layout-elk.esm.mjs";

window.mermaid = mermaid;   // expose globally
window.mermaid.registerLayoutLoaders(elkLayouts);

