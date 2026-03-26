import { Routes, Route } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import './components/ProtectedRoute.css'
import AppLayout from './layouts/AppLayout'
import HomePage from './pages/HomePage'
import Dashboard from './pages/Dashboard'
import Generator from './pages/Generator'
import MyPosts from './pages/MyPosts'
import MediaLibrary from './pages/MediaLibrary'
import CreatePostFlow from './pages/CreatePostFlow'
import Login from './pages/Login'
import Register from './pages/Register'

function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Public standalone pages (no sidebar) */}
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        {/* Protected app pages (with sidebar) */}
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={
            <ProtectedRoute><Dashboard /></ProtectedRoute>
          } />
          <Route path="/generate" element={
            <ProtectedRoute><Generator /></ProtectedRoute>
          } />
          <Route path="/posts" element={
            <ProtectedRoute><MyPosts /></ProtectedRoute>
          } />
          <Route path="/media" element={
            <ProtectedRoute><MediaLibrary /></ProtectedRoute>
          } />
          <Route path="/create-post" element={
            <ProtectedRoute><CreatePostFlow /></ProtectedRoute>
          } />
        </Route>
      </Routes>
    </AuthProvider>
  )
}

export default App
