const API = "";

function navigate(el) {
    document.querySelectorAll(".nav-item").forEach(n => n.classList.remove("active"));
    document.querySelectorAll(".section").forEach(s => s.classList.remove("active"));
    el.classList.add("active");
    const id = "section-" + el.dataset.section;
    document.getElementById(id).classList.add("active");
    const loaders = { dashboard: loadDashboard, agents: loadAgents, workflows: loadWorkflows, audit: loadAudit, events: loadEvents, connectors: loadConnectors };
    if (loaders[el.dataset.section]) loaders[el.dataset.section]();
}

function toast(msg, type) {
    const t = document.createElement("div");
    t.className = "toast toast-" + type;
    t.textContent = msg;
    document.getElementById("toasts").appendChild(t);
    setTimeout(() => t.remove(), 4000);
}

function fmt(n) {
    if (n >= 1e6) return "$" + (n / 1e6).toFixed(1) + "M";
    if (n >= 1e3) return "$" + (n / 1e3).toFixed(1) + "K";
    return "$" + n.toFixed(0);
}

function fmtTime(ticks) {
    if (!ticks) return "0h";
    var h = ticks / 36e9;
    if (h >= 24) return (h / 24).toFixed(1) + "d";
    if (h >= 1) return h.toFixed(1) + "h";
    return (h * 60).toFixed(0) + "m";
}

function deptBadge(d) {
    var colors = { Sales: "blue", Finance: "green", Legal: "purple", IT: "red", Procurement: "yellow", HR: "cyan" };
    return '<span class="badge badge-' + (colors[d] || "blue") + '">' + d + '</span>';
}

function riskBadge(r) {
    var m = { Low: "green", Medium: "yellow", High: "red", Critical: "red" };
    var labels = ["Low","Medium","High","Critical"];
    var label = labels[r] || r;
    return '<span class="badge badge-' + (m[label] || "blue") + '">' + label + '</span>';
}

function outcomeBadge(o) {
    var labels = ["Allowed","Denied","Escalated","Approved","AutoApproved","AuditOnly"];
    var colors = ["green","red","yellow","green","cyan","blue"];
    var i = typeof o === "number" ? o : 0;
    return '<span class="badge badge-' + colors[i] + '">' + labels[i] + '</span>';
}

function statusBadge(s) {
    var labels = ["Pending","Running","AwaitingApproval","Completed","Failed","Cancelled"];
    var colors = ["blue","cyan","yellow","green","red","red"];
    var i = typeof s === "number" ? s : 0;
    return '<span class="badge badge-' + colors[i] + '">' + labels[i] + '</span>';
}

// ========== DASHBOARD ==========
async function loadDashboard() {
    try {
        const r = await fetch(API + "/api/impact/dashboard");
        const d = await r.json();
        const s = d.summary || d.Summary || {};
        document.getElementById("dashboard-cards").innerHTML =
            '<div class="card"><div class="card-label">Total Cost Saved</div><div class="card-value" style="color:var(--green)">' + fmt(s.totalCostSaved || 0) + '</div><div class="card-sub">Across all departments</div></div>' +
            '<div class="card"><div class="card-label">Revenue Influenced</div><div class="card-value" style="color:var(--blue)">' + fmt(s.totalRevenueInfluenced || 0) + '</div><div class="card-sub">Deal acceleration + retention</div></div>' +
            '<div class="card"><div class="card-label">Time Saved</div><div class="card-value" style="color:var(--cyan)">' + fmtTime(s.totalTimeSaved ? (s.totalTimeSaved.ticks || 0) : 0) + '</div><div class="card-sub">Manual effort eliminated</div></div>' +
            '<div class="card"><div class="card-label">Workflows Executed</div><div class="card-value" style="color:var(--purple)">' + (s.totalWorkflowsExecuted || 0) + '</div><div class="card-sub">' + (s.totalStepsAutomated || 0) + ' steps automated</div></div>' +
            '<div class="card"><div class="card-label">Audit Entries</div><div class="card-value">' + (d.totalAuditEntries || 0) + '</div><div class="card-sub">' + (d.policyDenials || 0) + ' denials, ' + (d.humanEscalations || 0) + ' escalations</div></div>' +
            '<div class="card"><div class="card-label">Automation Rate</div><div class="card-value" style="color:var(--accent)">' + ((d.automationRate || 0) * 100).toFixed(0) + '%</div><div class="card-sub">Steps automated per workflow</div></div>';

        var bd = s.byDepartment || {};
        var keys = Object.keys(bd);
        if (keys.length === 0) {
            document.getElementById("department-table").innerHTML = '<div style="padding:20px;color:var(--text-dim);text-align:center">No department data yet. Execute a workflow to see impact.</div>';
        } else {
            var rows = keys.map(function(k) {
                var di = bd[k];
                return '<tr><td>' + deptBadge(di.department || k) + '</td><td style="color:var(--green)">' + fmt(di.costSaved || 0) + '</td><td style="color:var(--blue)">' + fmt(di.revenueInfluenced || 0) + '</td><td>' + fmtTime(di.timeSaved ? (di.timeSaved.ticks || 0) : 0) + '</td><td>' + (di.workflowsExecuted || 0) + '</td></tr>';
            }).join("");
            document.getElementById("department-table").innerHTML = '<table><thead><tr><th>Department</th><th>Cost Saved</th><th>Revenue</th><th>Time Saved</th><th>Workflows</th></tr></thead><tbody>' + rows + '</tbody></table>';
        }
    } catch (e) {
        document.getElementById("dashboard-cards").innerHTML = '<div style="padding:20px;color:var(--text-dim)">Dashboard will populate after workflows are executed.</div>';
        document.getElementById("department-table").innerHTML = '';
    }
}

// ========== AGENTS ==========
async function loadAgents() {
    const r = await fetch(API + "/api/agents");
    const agents = await r.json();
    var byDept = {};
    agents.forEach(function(a) { if (!byDept[a.department]) byDept[a.department] = []; byDept[a.department].push(a); });
    var html = '<div class="card-grid">';
    Object.keys(byDept).forEach(function(dept) {
        byDept[dept].forEach(function(a) {
            html += '<div class="card"><div style="display:flex;justify-content:space-between;align-items:start;margin-bottom:12px">' +
                '<div><div style="font-weight:600;font-size:15px">' + a.name + '</div><div style="font-size:12px;color:var(--text-dim);margin-top:2px">' + a.agentId + '</div></div>' +
                deptBadge(a.department) + '</div>' +
                '<div style="margin-bottom:8px">Risk: ' + riskBadge(a.riskTier) + '</div>' +
                '<div style="display:flex;flex-wrap:wrap;gap:4px">' +
                a.capabilities.map(function(c) { return '<span style="background:var(--surface2);padding:2px 8px;border-radius:4px;font-size:11px;color:var(--text-dim)">' + c + '</span>'; }).join("") +
                '</div></div>';
        });
    });
    html += '</div>';
    document.getElementById("agents-content").innerHTML = html;
}

// ========== WORKFLOWS ==========
async function loadWorkflows() {
    const r = await fetch(API + "/api/workflows");
    const workflows = await r.json();
    var html = '';
    workflows.forEach(function(w) {
        html += '<div class="table-card" style="margin-bottom:20px"><div class="table-card-header"><div><h2>' + w.name + '</h2>' +
            '<div style="font-size:12px;color:var(--text-dim);margin-top:2px">' + (w.description || "") + '</div></div>' +
            '<div style="display:flex;gap:8px;align-items:center">' + deptBadge(w.department) +
            '<button class="btn btn-sm btn-primary" onclick="showExecuteModal(\'' + w.workflowId + '\',\'' + w.name + '\')">&#9654; Execute</button></div></div>' +
            '<table><thead><tr><th>Step</th><th>Agent</th><th>Approval</th><th>Timeout</th></tr></thead><tbody>';
        w.steps.forEach(function(s) {
            var timeout = s.timeout ? s.timeout.replace("PT","").toLowerCase() : "-";
            html += '<tr><td><strong>' + s.name + '</strong><div style="font-size:11px;color:var(--text-dim)">' + s.stepId + '</div></td>' +
                '<td><code style="color:var(--cyan)">' + s.agentId + '</code></td>' +
                '<td>' + (s.requiresApproval ? '<span class="badge badge-yellow">Required</span>' : '<span class="badge badge-green">Auto</span>') + '</td>' +
                '<td>' + timeout + '</td></tr>';
        });
        html += '</tbody></table></div>';
    });
    document.getElementById("workflows-content").innerHTML = html;
}

function showExecuteModal(workflowId, name) {
    var presets = {
        "sales-stalled-deal-recovery": '{\n  "dealId": "OPP-48291",\n  "daysStalled": 11,\n  "amount": 480000\n}',
        "finance-month-end-reconciliation": '{}',
        "legal-contract-review": '{\n  "contractType": "SaaS Subscription",\n  "contractId": "CTR-9921"\n}',
        "it-security-incident-response": '{\n  "userId": "user-4521",\n  "anomalyScore": 85.0\n}',
        "procurement-vendor-negotiation": '{\n  "vendorId": "VENDOR-AWS",\n  "priceIncrease": 9.0\n}',
        "hr-attrition-prevention": '{\n  "teamId": "eng-platform"\n}'
    };
    document.getElementById("modal-root").innerHTML =
        '<div class="modal-overlay" onclick="if(event.target===this)closeModal()"><div class="modal">' +
        '<h3>Execute: ' + name + '</h3>' +
        '<div class="form-group"><label>Inputs (JSON)</label><textarea id="exec-inputs">' + (presets[workflowId] || "{}") + '</textarea></div>' +
        '<div id="exec-result"></div>' +
        '<div class="modal-actions"><button class="btn btn-outline" onclick="closeModal()">Close</button>' +
        '<button class="btn btn-primary" id="exec-btn" onclick="executeWorkflow(\'' + workflowId + '\')">&#9654; Execute</button></div></div></div>';
}

async function executeWorkflow(workflowId) {
    var btn = document.getElementById("exec-btn");
    btn.disabled = true;
    btn.textContent = "Executing...";
    try {
        var inputs = JSON.parse(document.getElementById("exec-inputs").value);
        var r = await fetch(API + "/api/workflows/" + workflowId + "/execute", {
            method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(inputs)
        });
        var result = await r.json();
        document.getElementById("exec-result").innerHTML = '<div style="margin-top:16px"><strong>Result:</strong> ' + statusBadge(result.status) + '</div><div class="json-viewer" style="margin-top:8px">' + JSON.stringify(result, null, 2) + '</div>';
        toast("Workflow executed successfully", "success");
    } catch (e) {
        document.getElementById("exec-result").innerHTML = '<div style="color:var(--red);margin-top:12px">Error: ' + e.message + '</div>';
        toast("Execution failed", "error");
    }
    btn.disabled = false;
    btn.textContent = "\u25B6 Execute";
}

function closeModal() { document.getElementById("modal-root").innerHTML = ""; }

// ========== AUDIT ==========
async function loadAudit() {
    try {
        var r = await fetch(API + "/api/audit?limit=50");
        var entries = await r.json();
        if (!entries.length) {
            document.getElementById("audit-content").innerHTML = '<div style="padding:20px;text-align:center;color:var(--text-dim)">No audit entries yet. Execute a workflow to generate audit trail.</div>';
            return;
        }
        var rows = entries.map(function(e) {
            return '<tr><td style="font-family:monospace;font-size:11px">' + (e.entryId || "").substring(0, 8) + '</td>' +
                '<td>' + e.action + '</td><td><code>' + e.agentId + '</code></td>' +
                '<td>' + outcomeBadge(e.outcome) + '</td>' +
                '<td style="font-size:11px;color:var(--text-dim)">' + (e.policyId || "-") + '</td>' +
                '<td style="font-size:11px;color:var(--text-dim)">' + new Date(e.timestamp).toLocaleString() + '</td></tr>';
        }).join("");
        document.getElementById("audit-content").innerHTML = '<table><thead><tr><th>ID</th><th>Action</th><th>Agent</th><th>Outcome</th><th>Policy</th><th>Time</th></tr></thead><tbody>' + rows + '</tbody></table>';
    } catch (e) {
        document.getElementById("audit-content").innerHTML = '<div style="padding:20px;color:var(--text-dim)">No audit data available.</div>';
    }
}

// ========== EVENTS ==========
function loadEvents() {
    var presets = [
        { label: "Sales: Stalled Deal", type: "sales.opportunity.stalled", dept: "Sales", payload: '{"dealId":"OPP-99182","daysStalled":14,"amount":320000}' },
        { label: "Finance: Month-End", type: "finance.month-end.triggered", dept: "Finance", payload: '{"period":"2024-Q4"}' },
        { label: "Legal: New Contract", type: "legal.contract.received", dept: "Legal", payload: '{"contractId":"CTR-1024","contractType":"Enterprise License"}' },
        { label: "IT: Security Anomaly", type: "it.security.anomaly-detected", dept: "IT", payload: '{"userId":"user-8821","anomalyScore":92}' },
        { label: "Procurement: Price Increase", type: "procurement.vendor.price-increase", dept: "Procurement", payload: '{"vendorId":"VENDOR-AWS","priceIncrease":9.0}' },
        { label: "HR: Attrition Spike", type: "hr.attrition.risk-spike", dept: "HR", payload: '{"teamId":"eng-platform"}' }
    ];
    var btns = presets.map(function(p) {
        return '<button class="btn btn-outline" style="margin-bottom:8px" onclick=\'publishEvent(' + JSON.stringify(p) + ')\'>' + p.label + '</button>';
    }).join(" ");
    document.getElementById("events-content").innerHTML =
        '<div class="card" style="margin-bottom:20px"><div class="card-label">Quick Publish</div><div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:8px">' + btns + '</div></div>' +
        '<div class="table-card"><div class="table-card-header"><h2>Custom Event</h2></div><div style="padding:20px">' +
        '<div class="form-group"><label>Event Type</label><input id="evt-type" value="sales.opportunity.stalled"></div>' +
        '<div class="form-group"><label>Department</label><input id="evt-dept" value="Sales"></div>' +
        '<div class="form-group"><label>Source</label><input id="evt-source" value="Dashboard"></div>' +
        '<div class="form-group"><label>Payload (JSON)</label><textarea id="evt-payload">{"dealId":"OPP-100","amount":250000}</textarea></div>' +
        '<button class="btn btn-primary" onclick="publishCustomEvent()">&#9889; Publish Event</button>' +
        '<div id="evt-result" style="margin-top:12px"></div></div></div>';
}

async function publishEvent(p) {
    try {
        var evt = { source: "Dashboard", eventType: p.type, department: p.dept, severity: 2, payload: JSON.parse(p.payload) };
        var r = await fetch(API + "/api/events", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(evt) });
        var result = await r.json();
        toast("Event published: " + p.type, "success");
    } catch (e) {
        toast("Failed to publish event", "error");
    }
}

async function publishCustomEvent() {
    try {
        var evt = {
            source: document.getElementById("evt-source").value,
            eventType: document.getElementById("evt-type").value,
            department: document.getElementById("evt-dept").value,
            severity: 2,
            payload: JSON.parse(document.getElementById("evt-payload").value)
        };
        var r = await fetch(API + "/api/events", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(evt) });
        var result = await r.json();
        document.getElementById("evt-result").innerHTML = '<div class="json-viewer">' + JSON.stringify(result, null, 2) + '</div>';
        toast("Event published", "success");
    } catch (e) {
        toast("Failed: " + e.message, "error");
    }
}

// ========== CONNECTORS ==========
async function loadConnectors() {
    try {
        var r = await fetch(API + "/api/connectors/health");
        var connectors = await r.json();
        var html = '<div class="card-grid">';
        connectors.forEach(function(c) {
            html += '<div class="card"><div style="display:flex;align-items:center;gap:10px;margin-bottom:12px">' +
                '<span class="status-dot ' + (c.isHealthy ? "healthy" : "unhealthy") + '"></span>' +
                '<span style="font-weight:600;font-size:16px">' + c.systemName + '</span></div>' +
                '<div style="font-size:13px;color:var(--text-dim)">' + (c.message || "") + '</div>' +
                '<div style="font-size:12px;color:var(--text-dim);margin-top:8px">Latency: ' + (c.latency || 0).toFixed(0) + 'ms</div></div>';
        });
        html += '</div>';
        document.getElementById("connectors-content").innerHTML = html;
    } catch (e) {
        document.getElementById("connectors-content").innerHTML = '<div style="padding:20px;color:var(--text-dim)">Could not load connector health.</div>';
    }
}

// Init
loadDashboard();